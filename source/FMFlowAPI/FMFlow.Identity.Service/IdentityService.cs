using EFRepository;
using FluentValidation;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Identity.Interface;
using FMFlow.Identity.Interface.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.Identity.Service;

public class IdentityService(
	IIdentityRepository identityRepository,
	IRepository repository,
	IValidator<SavePasswordRequestDto> savePasswordValidator)
	: IIdentityService
{
	public async Task<Result> AssignUserToRole(int userID, Roles role, CancellationToken ct)
	{
		var foundUser = await GetUserByUserId(userID, false, ct);

		if (foundUser == null)
		{
			return Result.Failure($"User not found", ResultErrorType.NotFound);
		}

		var keycloakRole = await identityRepository.GetRoleByName(role, ct);

		if (keycloakRole == null)
		{
			return Result.Failure("Role not found", ResultErrorType.NotFound);
		}

		await identityRepository.AssignRoleToUser(foundUser.IdentityGuid, keycloakRole, ct);

		return Result.Success();
	}

	public async Task<Result<CreateTempCustomerDto>> CreateTempCustomer(FlowUser newCustomerUser, int leadId, CancellationToken ct)
	{
		if (newCustomerUser == null)
		{
			return Result<CreateTempCustomerDto>.Failure($"{nameof(newCustomerUser)} is required");
		}

		ct.ThrowIfCancellationRequested();

		// Ensure temp FlowUser doesn't already exist
		if (await DoesUserExistByEmail(newCustomerUser.Email, ct))
		{
			return Result<CreateTempCustomerDto>.Failure("Email already exists.");
		}

		newCustomerUser.UserID = null;
		var randomPassword = GenerateRandomPassword();
		var createUserResult = await CreateUser(newCustomerUser, ct, leadId, randomPassword);

		if (!createUserResult.IsSuccess)
		{
			return Result<CreateTempCustomerDto>.Failure(createUserResult.Error!);
		}

		newCustomerUser.UserID = createUserResult.Value;

		// Assign TempCustomer role
		var keycloakRole = await identityRepository.GetRoleByName(Roles.TempCustomer, ct);

		if (keycloakRole == null)
		{
			return Result<CreateTempCustomerDto>.Failure("Keycloak role not found", ResultErrorType.NotFound);
		}

		await identityRepository.AssignRoleToUser(newCustomerUser.IdentityGuid, keycloakRole, ct);

		// Authenticate user
		var token = await identityRepository.AuthenticateUser(newCustomerUser.Email, randomPassword, ct);

		if (token == null || string.IsNullOrEmpty(token.AccessToken))
		{
			return Result<CreateTempCustomerDto>.Failure("Failed to authenticate user");
		}

		var tempCustomerDto = new CreateTempCustomerDto
		{
			CustomerUserID = newCustomerUser.UserID.Value,
			Token = token
		};

		return Result<CreateTempCustomerDto>.Success(tempCustomerDto);
	}

	public async Task<Result<int>> CreateUser(FlowUser user, CancellationToken ct, int? leadId, string? password = null)
	{
		if (user == null)
		{
			return Result<int>.Failure("Email already exists.");
		}

		if (await DoesUserExistByEmail(user.Email, ct))
		{
			return Result<int>.Failure("Email already exists.");
		}

		if (user.UserID == 0)
		{
			user.UserID = null;
		}

		repository.AddNew(user);
		await repository.SaveAsync(ct);

		if (!user.UserID.HasValue)
		{
			throw new InvalidOperationException("Failed to save user to the database.");
		}

		if (string.IsNullOrEmpty(password))
		{
			password = GenerateRandomPassword();
		}

		var identityUserID = await identityRepository.CreateUser(
			user.Email,
			password,
			user.FirstName,
			user.LastName,
			user.UserID,
			leadId,
			ct);

		if (!identityUserID.HasValue)
		{
			throw new InvalidOperationException("Failed to create user in the identity repository.");
		}

		user.IdentityGuid = identityUserID.Value;
		repository.AddOrUpdate(user);
		await repository.SaveAsync(ct);

		return Result<int>.Success(user.UserID.Value);
	}

	public async Task<Result<TokenResponseDto>> SaveUserPassword(int userID, string password, CancellationToken ct)
	{
		var foundUser = await GetUserByUserId(userID, false, ct);

		if (foundUser == null)
		{
			return Result<TokenResponseDto>.Failure(ErrorMessages.UserNotFound, ResultErrorType.NotFound);
		}

		await identityRepository.SetUserPassword(foundUser.IdentityGuid.ToString(), password, ct);

		var response = await identityRepository.AuthenticateUser(foundUser.Email, password, ct);

		if (response == null || string.IsNullOrEmpty(response.AccessToken))
			return Result<TokenResponseDto>.Failure("Failed to authenticate user", ResultErrorType.PermissionDenied);

		return Result<TokenResponseDto>.Success(response);
	}

	private IQueryable<FlowUser> GetActiveUsers()
	{
		return repository.Query<FlowUser>()
			.ByIsDeleted(false)
			.AsNoTracking();
	}

	private async Task<FlowUser?> GetUserByUserId(int userID, bool includeDeletedUsers, CancellationToken ct)
	{
		var query = repository.Query<FlowUser>()
			.ByUserID(userID)
			.AsNoTracking();

		if (!includeDeletedUsers)
		{
			query = query.ByIsDeleted(false);
		}

		return await query.FirstOrDefaultAsync(ct);
	}

	public async Task<FlowUser?> GetUserProfileByEmail(string email, CancellationToken ct)
	{
		return await GetActiveUsers()
			.Include(u => u.ProUser)
			.Include(u => u.EmployeeUser)
			.ThenInclude(e => e!.Address)
			.Include(u => u.EmployeeUser)
			.ThenInclude(eu => eu!.AssignedZipCodes)
			.FirstOrDefaultAsync(u => u != null && u.Email.ToLower() == email.ToLower().Trim(), ct);
	}

	private async Task<bool> DoesUserExistByEmail(string email, CancellationToken ct)
	{
		return await GetActiveUsers().AnyAsync(u => u.Email.ToLower() == email.ToLower().Trim(), ct);
	}

	private static string GenerateRandomPassword()
	{
		var random = new Random();
		return random.Next(10000, 99999 + 1).ToString();
	}

	public async Task<Result> UpdateUserDeletedStatus(int userID, bool isDeleted, CancellationToken ct)
	{
		var foundUser = await GetUserByUserId(userID, true, ct);

		if (foundUser == null)
		{
			return Result.Failure($"{nameof(userID)} does not exist");
		}

		foundUser.IsDeleted = isDeleted;

		if (isDeleted)
		{
			foundUser.DateDeleted = DateTime.UtcNow;
		}
		else
		{
			foundUser.DateDeleted = null;
		}

		repository.AddOrUpdate(foundUser);
		await repository.SaveAsync(ct);

		return Result.Success();
	}

	public async Task<Result> RemoveRoleFromUser(int userID, Roles role, CancellationToken ct)
	{
		var foundUser = await GetUserByUserId(userID, false, ct);

		if (foundUser == null)
		{
			return Result.Failure("User not found", ResultErrorType.NotFound);
		}

		var keycloakRole = await identityRepository.GetRoleByName(role, ct);

		if (keycloakRole == null)
		{
			return Result.Failure("Role not found", ResultErrorType.NotFound);
		}

		await identityRepository.RemoveRoleFromUser(foundUser.IdentityGuid, keycloakRole, ct);

		return Result.Success();
	}

	public async Task<Result<TokenResponseDto>> AuthenticateServiceClient(ServiceAccountClientTokenRequestDto request, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.ClientSecret))
		{
			return Result<TokenResponseDto>.Failure("ClientId and ClientSecret are required", ResultErrorType.BadRequest);
		}

		var tokenResponse = await identityRepository.AuthenticateServiceClient(request.ClientId, request.ClientSecret, ct);

		if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
		{
			return Result<TokenResponseDto>.Failure("Failed to authenticate service client", ResultErrorType.Unauthorized);
		}

		return Result<TokenResponseDto>.Success(tokenResponse);
	}
}
