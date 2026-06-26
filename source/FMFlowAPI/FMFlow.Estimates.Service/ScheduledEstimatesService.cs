using EFRepository;
using FluentValidation;
using FMFlow.AccessValidation;
using FMFlow.Common;
using FMFlow.Email.Interface;
using FMFlow.Entities;
using FMFlow.Estimates.Interface;
using FMFlow.Estimates.Interface.DTOs;
using FMFlow.Estimates.Service.Mapper;
using FMFlow.FlowAPI.Interface;
using FMFlow.Identity.Interface;
using FMFlow.ProUser.Interface;
using FMFlow.SMS.Interface;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.Estimates.Service;

public class ScheduledEstimatesService(
	IRepository repository,
	IValidator<ScheduledEstimateRequestDto> requestValidator,
	IAccessValidator accessValidator,
	ICurrentUserService currentUserService,
	IEmailSenderService emailSenderService,
	ISMSSenderService smsSenderService,
	ICustomerTempProsService customerTempProsService) : IScheduledEstimatesService
{
	public async Task<Result<ScheduledEstimateResponseDto>> CreateScheduledEstimate(ScheduledEstimateRequestDto request, CancellationToken ct, bool isBatch = false)
	{
		var requestValidation = await DtoValidator.Validate(request, requestValidator, ct);

		if (!requestValidation.IsSuccess)
			return Result<ScheduledEstimateResponseDto>.Failure(requestValidation.Error!);

		// Retrieve the project with related data
		var project = await repository
			.Query<Project>()
			.ByProjectID(request.ProjectID)
			.Include(p => p.Lead)
				.ThenInclude(l => l.Customer)
			.Include(p => p.Address)
			.Include(p => p.RequestedEstimates)
			.FirstOrDefaultAsync(ct);

		if (project == null)
			return Result<ScheduledEstimateResponseDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		if (currentUserService.IsTempCustomer())
		{
			if (project.LeadID != currentUserService.GetLeadId())
				return Result<ScheduledEstimateResponseDto>.Failure(ErrorMessages.ResourceAccessDenied);
		}
		else
		{
			var accessResult = await accessValidator.ValidateAccessToProject(request.ProjectID, ct);

			if (!accessResult.IsSuccess)
				return Result<ScheduledEstimateResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);
		}

		if (currentUserService.IsCustomer() || currentUserService.IsTempCustomer())
		{
			var proId = await customerTempProsService.GetProId(request.ProUserID, ct);

			if (!proId.HasValue)
			{
				return Result<ScheduledEstimateResponseDto>.Failure("Cannot find pro ID.");
			}

			request = request with { ProUserID = proId.Value };
		}

		if (currentUserService.IsPro())
		{
			if (currentUserService.GetUserID() != request.ProUserID)
			{
				return Result<ScheduledEstimateResponseDto>.Failure("You are not authorized to create a scheduled estimate for another user.");
			}

			if (project.Lead.IsReferralSource)
			{
				smsSenderService.SendSmsCustomerProCreatedScheduledEstimate(project.ProjectID, project.Lead.Customer.PhoneNumber);
			}
		}

		var proUser = await repository
			.Query<ProUserDetail>()
			.Include(pu => pu.FMTimeZone)
			.Include(pu => pu.FlowUser)
			.ByUserID(request.ProUserID)
			.FirstOrDefaultAsync(ct);

		if (proUser is null)
			return Result<ScheduledEstimateResponseDto>.Failure("Pro user not found.");

		if (proUser.FlowUser?.IsDeleted == true)
			return Result<ScheduledEstimateResponseDto>.Failure("Cannot schedule a deactivated pro.");

		if (currentUserService.IsAccountManager())
		{
			if (string.IsNullOrWhiteSpace(request.UserTimeZone) || !TimeZoneInfo.TryConvertIanaIdToWindowsId(request.UserTimeZone!, out string? windowsTimeZoneId))
			{
				return Result<ScheduledEstimateResponseDto>.Failure("Invalid or missing time zone.");
			}

			var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(windowsTimeZoneId);
			var proTimeZone = TimeZoneInfo.FindSystemTimeZoneById(proUser.FMTimeZone!.SystemTimeZoneId);

			var updatedTime = TimeZoneHelper.PreserveTimeInDifferentTimeZone(request.ScheduledDateTime, userTimeZone, proTimeZone);

			request = request with { ScheduledDateTime = updatedTime };
		}

		if (project.ProId != request.ProUserID &&
			project.Lead.ProUserID != request.ProUserID)
		{
			var zipCodesAssignedToPro = await repository
				.Query<ProUserToProZipcode>()
				.ByUserID(request.ProUserID)
				.Select(x => x.Zipcode)
				.ToListAsync(ct);

			if (zipCodesAssignedToPro == null || !zipCodesAssignedToPro.Contains(project!.Address.ZipCode))
				return Result<ScheduledEstimateResponseDto>.Failure("Pro user not assigned to project zip code.");
		}

		var mapper = new ScheduledEstimateMapper();
		var scheduledEstimate = mapper.MapToScheduledEstimate(request);

		if (project.IsOpen)
		{
			if (request.RequestedEstimateId is not null)
			{
				// Create single Estimate if requestedEstimateId is provided
				var requestedEstimate = await repository
					.Query<RequestedEstimate>()
					.ByRequestedEstimateID(request.RequestedEstimateId)
					.FirstAsync(ct);

				var estimate = new Estimate
				{
					ProUserID = request.ProUserID,
					RequestedEstimate = requestedEstimate,
					ScheduledEstimate = scheduledEstimate,
					StatusLastUpdate = DateTimeOffset.UtcNow
				};

				repository.AddOrUpdate(estimate);
			}
			else
			{
				// If no requested estimateId is provided create estimates for all existing requested estimates
				foreach (var requestedEstimate in project.RequestedEstimates)
				{
					var estimate = new Estimate
					{
						ProUserID = request.ProUserID,
						RequestedEstimate = requestedEstimate,
						ScheduledEstimate = scheduledEstimate,
						StatusLastUpdate = DateTimeOffset.UtcNow
					};

					repository.AddOrUpdate(estimate);
				}
			}
		}

		if (!project.Lead.CanSetScheduleComplete)
			project.Lead.CanSetScheduleComplete = true;

		// Convert request value to UTC since Npgsql only supports DateTimeOffset as UTC
		scheduledEstimate.ScheduledDateTime = scheduledEstimate.ScheduledDateTime.ToUniversalTime();

		scheduledEstimate.ProUser = await repository.Query<FlowUser>()
			.ByUserID(scheduledEstimate.ProUserID)
			.Include(p => p.ProUser)
			.FirstAsync(ct);

		repository.AddOrUpdate(scheduledEstimate);
		await repository.SaveAsync(ct);

		if (!isBatch)
		{
			await emailSenderService.SendEmailCustomerResidentialEstimateScheduled([scheduledEstimate.ScheduledEstimateID], ct);
		}

		var response = ScheduledEstimateMapper.MapToScheduledEstimateResponse(scheduledEstimate);
		return Result<ScheduledEstimateResponseDto>.Success(response);
	}

	public async Task<Result<ScheduledEstimateResponseDto>> GetScheduledEstimate(int scheduledEstimateId, CancellationToken ct)
	{
		var scheduledEstimate = await repository
			.Query<ScheduledEstimate>()
			.Where(x => x.ScheduledEstimateID == scheduledEstimateId)
			.Include(se => se.ProUser!.ProUser!.FMTimeZone)
			.Include(se => se.Project)
			.FirstOrDefaultAsync(ct);

		if (scheduledEstimate == null)
			return Result<ScheduledEstimateResponseDto>.Failure("Scheduled estimate not found.", ResultErrorType.NotFound);

		if (scheduledEstimate.IsDeleted)
			return Result<ScheduledEstimateResponseDto>.Failure("Scheduled estimate was previously deleted.");

		var accessResult = await accessValidator.ValidateAccessToProject(scheduledEstimate, ct);

		if (!accessResult.IsSuccess)
			return Result<ScheduledEstimateResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);

		// Convert to pro user's time zone
		var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(scheduledEstimate.ProUser.ProUser!.FMTimeZone!.SystemTimeZoneId);
		scheduledEstimate.ScheduledDateTime = TimeZoneInfo.ConvertTime(scheduledEstimate.ScheduledDateTime, timeZoneInfo);

		var response = ScheduledEstimateMapper.MapToScheduledEstimateResponse(scheduledEstimate);

		return Result<ScheduledEstimateResponseDto>.Success(response);
	}

	public async Task<Result<SearchResult<ScheduledEstimateResponseDto>>> SearchScheduledEstimates(
		int projectID,
		int pageIndex,
		int pageSize,
		CancellationToken ct)
	{
		if (pageIndex < 0 || pageSize < 1)
			return Result<SearchResult<ScheduledEstimateResponseDto>>.Failure("Invalid pagination parameters.");

		pageSize = Math.Min(pageSize, 50);

		if (projectID <= 0)
			return Result<SearchResult<ScheduledEstimateResponseDto>>.Failure("Project ID must be greater than 0.", ResultErrorType.BadRequest);

		var query = repository
			.Query<ScheduledEstimate>()
			.ByIsDeleted(false)
			.ByProjectID(projectID);

		if (currentUserService.IsPro())
			query = query.Where(x => x.ProUserID == currentUserService.GetUserID());

		if (currentUserService.IsAccountManager())
		{
			var currentUser = await currentUserService.GetCurrentUser(ct);

			if (currentUser == null)
				return Result<SearchResult<ScheduledEstimateResponseDto>>.Failure("Current user not found.", ResultErrorType.NotFound);

			var assignedZipCodes = currentUser.EmployeeUser!.AssignedZipCodes.Select(z => z.Zipcode).ToList();

			query = query.Where(x => assignedZipCodes.Contains(x.Project.Address.ZipCode));
		}

		var totalResults = await query.CountAsync(ct);

		var scheduledEstimates = await query
			.Skip(pageIndex * pageSize)
			.Take(pageSize)
			.OrderByDescending(x => x.ScheduledDateTime)
			.Include(se => se.ProUser!.ProUser!.FMTimeZone)
			.ToListAsync(ct);

		var scheduledEstimateDtos = scheduledEstimates
			.Select(ScheduledEstimateMapper.MapToScheduledEstimateResponse)
			.ToList();

		var searchResult = new SearchResult<ScheduledEstimateResponseDto>(scheduledEstimateDtos, totalResults);

		return Result<SearchResult<ScheduledEstimateResponseDto>>.Success(searchResult);
	}

	public async Task<Result<ScheduledEstimateResponseDto>> UpdateScheduledEstimate(int scheduledEstimateId, ScheduledEstimateUpdateRequestDto request, CancellationToken ct)
	{
		if (request.ScheduledDateTime < DateTimeOffset.UtcNow)
			return Result<ScheduledEstimateResponseDto>.Failure("Scheduled date time cannot be in the past.");

		var scheduledEstimate = await repository
			.Query<ScheduledEstimate>()
			.ByScheduledEstimateID(scheduledEstimateId)
			.FirstOrDefaultAsync(ct);

		if (scheduledEstimate == null)
			return Result<ScheduledEstimateResponseDto>.Failure("Scheduled estimate not found.", ResultErrorType.NotFound);

		if (scheduledEstimate!.IsDeleted)
			return Result<ScheduledEstimateResponseDto>.Failure("Scheduled estimate was previously deleted.");

		var accessResult = await accessValidator.ValidateAccessToProject(scheduledEstimate, ct);

		if (!accessResult.IsSuccess)
			return Result<ScheduledEstimateResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);

		// Convert request value to UTC since Npgsql only supports DateTimeOffset as UTC
		scheduledEstimate!.ScheduledDateTime = request.ScheduledDateTime.ToUniversalTime();

		scheduledEstimate.ProUser = await repository.Query<FlowUser>()
			.ByUserID(scheduledEstimate.ProUserID)
			.FirstAsync(ct);

		repository.AddOrUpdate(scheduledEstimate);
		await repository.SaveAsync(ct);

		var response = ScheduledEstimateMapper.MapToScheduledEstimateResponse(scheduledEstimate);

		return Result<ScheduledEstimateResponseDto>.Success(response);
	}

	public async Task<Result> DeleteScheduledEstimate(int scheduledEstimateId, CancellationToken ct)
	{
		var scheduledEstimate = await repository
			.Query<ScheduledEstimate>()
			.ByScheduledEstimateID(scheduledEstimateId)
			.Include(se => se.Project)
				.ThenInclude(p => p.Lead)
			.Include(se => se.Project)
				.ThenInclude(p => p.ScheduledEstimates.Where(se => se.IsDeleted == false))
			.FirstOrDefaultAsync(ct);

		if (scheduledEstimate == null)
			return Result.Failure("Scheduled estimate not found.", ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToProject(scheduledEstimate, ct);

		if (!accessResult.IsSuccess)
			return Result.Failure(accessResult.Error!, accessResult.ErrorType);

		if (scheduledEstimate!.IsDeleted)
			return Result.Failure("Scheduled estimate already deleted.");

		scheduledEstimate.IsDeleted = true;
		scheduledEstimate.DateDeleted = DateTimeOffset.UtcNow;

		scheduledEstimate.ProUser = await repository.Query<FlowUser>()
			.ByUserID(scheduledEstimate.ProUserID)
			.FirstAsync(ct);

		var leadHasOtherScheduledEstimates = scheduledEstimate.Project.ScheduledEstimates
			.Any(se => se.ScheduledEstimateID != scheduledEstimate.ScheduledEstimateID);

		if (!leadHasOtherScheduledEstimates)
			scheduledEstimate.Project.Lead.CanSetScheduleComplete = false;

		repository.AddOrUpdate(scheduledEstimate);
		await repository.SaveAsync(ct);

		return Result.Success();
	}
}
