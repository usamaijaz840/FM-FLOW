using System.Security.Claims;
using EFRepository;
using FluentValidation;
using FMFlow.Common;
using FMFlow.Customers.Interface;
using FMFlow.Customers.Interface.DTOs;
using FMFlow.Customers.Service.Mapper;
using FMFlow.Email.Interface;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Identity;
using FMFlow.Identity.Interface;
using FMFlow.Identity.Interface.DTOs;
using FMFlow.LeadTimelines.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FMFlow.Customers.Service;

public class CustomersService(IRepository repository,
	IEmailService emailService,
	IIdentityService identityService,
	IValidator<NonceRequestDto> nonceValidator,
	INonceService nonceService,
	IValidator<CustomerRequestDto> customerRequestDtoValidator,
	IValidator<CustomerLeadRequestDto> customerLeadRequestValidator,
	ILeadTimelineService leadTimelineService,
	ICustomJwtService customJwtService,
	IOptions<CustomJwtConfiguration> customJwtOptions)
	: ICustomersService
{
	private readonly CustomJwtConfiguration _customJwtConfig = customJwtOptions.Value;

	public async Task<Result> FinalizeCustomerActivation(CustomerRequestDto request, CancellationToken ct)
	{
		Result result = await customerRequestDtoValidator.ValidateWithResult(request, ct)
			.MapResult(async (CustomerRequestDto validatedRequest, CancellationToken ct2) => await nonceService.ValidateAndConsumeNonce(validatedRequest.nonce!, ct2), ct)
			.MapResult(FindLeadByID, ct)
			.MapResult((Lead lead, CancellationToken ct) => CreateCustomerIfNeeded(request, lead, ct), ct)
			.MapResult(AdjustCustomerRoles, ct)
			.MapResult(async (Lead lead, CancellationToken ct) =>
			{
				repository.AddOrUpdate(lead);
				await repository.SaveAsync(ct);
				return Result.Success();
			}, ct);

		return result;
	}

	private async Task<Result<Lead>> CreateCustomerIfNeeded(CustomerRequestDto request, Lead foundLead, CancellationToken ct)
	{
		if (foundLead.CustomerID.HasValue && foundLead.Customer != null)
		{
			await identityService.SaveUserPassword(foundLead.CustomerID.Value, request.Password, ct);

			return Result<Lead>.Success(foundLead);
		}

		var user = new FlowUser
		{
			Email = foundLead.Email,
			FirstName = foundLead.FirstName,
			LastName = foundLead.LastName,
		};

		Result<Lead> createUserResult = await identityService.CreateUser(
			info: user,
			ct,
			leadId: foundLead.LeadID,
			password: request.Password
		).MapResult(async (int userId, CancellationToken ct) =>
		{

			foundLead.CustomerID = userId;
			return Result<Lead>.Success(foundLead);

		}, ct);

		return createUserResult;
	}
	private async Task<Result<Lead>> FindLeadByID(Nonce nonce, CancellationToken ct)
	{
		int leadId = nonce.EntityId;

		Lead? foundLead = await repository.Query<Lead>()
			.ByLeadID(leadId)
			.FirstOrDefaultAsync(cancellationToken: ct);

		if (foundLead == null)
			return Result<Lead>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		return Result<Lead>.Success(foundLead);
	}

	private async Task<Result<Lead>> AdjustCustomerRoles(Lead lead, CancellationToken ct)
	{
		Result<Lead> result = await identityService.AssignUserToRole(lead.CustomerID!.Value, Roles.Customer, ct)
			.MergeResult(async ct2 => await identityService.RemoveRoleFromUser(lead.CustomerID!.Value, Roles.TempCustomer, ct2), ct)
			.MapResult(() => Result<Lead>.Success(lead), ct);

		return result;
	}

	public async Task<Result<NonceResponseDto>> VerifyNonce(NonceRequestDto request, CancellationToken ct)
	{
		Result<NonceResponseDto> result = await nonceValidator.ValidateWithResult(request, ct)
				.MapResult(async (NonceRequestDto req, CancellationToken ct) =>
				{
					Result nonceValidationResult = await nonceService.ValidateNonce(request.nonce!, ct);

					if (!nonceValidationResult.IsSuccess)
						return Result<NonceResponseDto>.Success(new NonceResponseDto { IsValid = false });

					return Result<NonceResponseDto>.Success(new NonceResponseDto { IsValid = true });

				}, ct);

		return result;
	}

	public async Task<Result<CustomerLeadResponseDto>> CreateCustomerLead(CustomerLeadRequestDto request, CancellationToken ct)
	{
		var result = await customerLeadRequestValidator.ValidateWithResult(request, ct)
			.MapResult(ValidateUserNotAlreadyExists, ct)
			.MapResult(ValidateState, ct)
			.MapResult(ValidateLeadSource, ct)
			.MapResult(CreateCustomer, ct)
			.MapResult(CreateLead, ct)
			.MapResult(CreateToken, ct)
			.MapResult(BuildResponse, ct);

		return result;
	}

	private async Task<Result<CustomerLeadRequestDto>> ValidateUserNotAlreadyExists(CustomerLeadRequestDto request, CancellationToken ct)
	{
		var existingUser = await repository
			.Query<FlowUser>()
			.Where(u => u.Email.ToLower() == request.Email.ToLower())
			.FirstOrDefaultAsync(ct);

		if (existingUser != null)
			return Result<CustomerLeadRequestDto>.Failure("User with specified email already exists.", ResultErrorType.BadRequest);

		return Result<CustomerLeadRequestDto>.Success(request);
	}

	private async Task<Result<CustomerLeadRequestDto>> ValidateState(CustomerLeadRequestDto request, CancellationToken ct)
	{
		var state = await repository
			.Query<State>()
			.ByAbbreviation(request.State)
			.FirstOrDefaultAsync(ct);

		if (state == null)
			return Result<CustomerLeadRequestDto>.Failure("State not found.", ResultErrorType.NotFound);

		return Result<CustomerLeadRequestDto>.Success(request);
	}

	private async Task<Result<CustomerLeadRequestDto>> ValidateLeadSource(CustomerLeadRequestDto request, CancellationToken ct)
	{
		if (!request.LeadSourceID.HasValue)
			return Result<CustomerLeadRequestDto>.Success(request);

		var leadSource = await repository
			.Query<LeadSource>()
			.ByLeadSourceID(request.LeadSourceID.Value)
			.FirstOrDefaultAsync(ct);

		if (leadSource == null)
			return Result<CustomerLeadRequestDto>.Failure("Lead source not found.", ResultErrorType.NotFound);

		return Result<CustomerLeadRequestDto>.Success(request);
	}

	private Result<(CustomerLeadRequestDto, FlowUser)> CreateCustomer(CustomerLeadRequestDto dto)
	{
		var createdDate = DateTimeOffset.UtcNow;
		var flowUser = CustomerLeadMapper.MapToFlowUser(dto, createdDate);

		return Result<(CustomerLeadRequestDto, FlowUser)>.Success((dto, flowUser));
	}

	private async Task<Result<Lead>> CreateLead((CustomerLeadRequestDto, FlowUser) tuple, CancellationToken ct)
	{
		var (dto, flowUser) = tuple;

		var createdDate = DateTimeOffset.UtcNow;
		var lead = CustomerLeadMapper.MapToLead(dto, createdDate);

		lead.Customer = flowUser;

		repository.AddNew(lead);
		await repository.SaveAsync(ct);

		// Record timeline with explicit user ID since user is not authenticated
		await leadTimelineService.RecordLeadTimelineAsync(lead, TimelineEventKey.LeadCreated, flowUser.UserID!.Value, ct);

		return Result<Lead>.Success(lead);
	}

	private async Task<Result<(Lead, TokenResponseDto)>> CreateToken(Lead leadWithCustomer, CancellationToken ct)
	{
		var claims = new[] {
			new Claim(CustomClaimTypes.ExternalId, leadWithCustomer.Customer.UserID.ToString()!),
			new Claim(CustomClaimTypes.PreferredUsername, leadWithCustomer.Customer.Email),
			new Claim(ClaimTypes.Role, nameof(Roles.Customer)),
			new Claim(CustomClaimTypes.TokenPurpose, "onboarding")
		};

		var result = customJwtService.GenerateCustomJwt(claims, _customJwtConfig.CustomerOnboardingTokenExpirationMinutes)
			.MapResult(tokenString =>
			{
				var tokenDto = new TokenResponseDto
				{
					AccessToken = tokenString,
					TokenType = "Bearer",
				};

				return Result<(Lead, TokenResponseDto)>.Success((leadWithCustomer, tokenDto));
			});

		return result;
	}

	private Result<CustomerLeadResponseDto> BuildResponse((Lead, TokenResponseDto) tuple)
	{
		var (lead, token) = tuple;
		var responseDto = CustomerLeadMapper.MapToResponse(lead, token);

		return Result<CustomerLeadResponseDto>.Success(responseDto);
	}
}
