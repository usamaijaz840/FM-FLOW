using System.Security.Claims;
using EFRepository;
using FluentValidation;
using FMFlow.Admin.Service.Mappers;
using FMFlow.Common;
using FMFlow.Common.Extensions;
using FMFlow.Email.Interface;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Identity;
using FMFlow.Identity.Interface;
using FMFlow.Identity.Interface.DTOs;
using FMFlow.Login.Interface;
using FMFlow.Login.Interface.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FMFlow.Login.Service;

public class LoginService(
	IRepository repository,
	IIdentityRepository identityRepository,
	IIdentityService identityService,
	IValidator<RefreshTokenRequestDto> refreshValidator,
	IValidator<ResetPasswordRequestDto> resetPasswordValidator,
	IValidator<MagicLinkDto> magicLinkValidator,
	IValidator<SavePasswordRequestDto> savePasswordValidator,
	ICurrentUserService currentUserService,
	ILogger<LoginService> logger,
	INonceService nonceService,
	ICustomJwtService customJwtService,
	IEmailSenderService emailSenderService,
	IOptions<CustomJwtConfiguration> customJwtOptions,
	IConfiguration config) : ILoginService
{
	readonly string _webAppBaseUrl = config["WebAppBaseUrl"] ?? "http://localhost:3000";
	readonly CustomJwtConfiguration _customJwtConfig = customJwtOptions.Value;

	public async Task<Result<AuthenticateUserResponseDto>> AuthenticateUser(AuthenticateUserRequestDto request, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
		{
			return Result<AuthenticateUserResponseDto>.Failure("Email and password are required");
		}

		var response = await identityRepository.AuthenticateUser(request.Email, request.Password, ct);

		if (response == null)
		{
			return Result<AuthenticateUserResponseDto>.Failure("Authentication failed", ResultErrorType.Unauthorized);
		}

		var foundUser = await identityService.GetUserProfileByEmail(request.Email, ct);

		if (foundUser is null || foundUser.IsDeleted)
		{
			logger.LogError($"User not found for email: {request.Email}");
			return Result<AuthenticateUserResponseDto>.Failure(ErrorMessages.UserNotFound, ResultErrorType.NotFound);
		}

		if (foundUser.ProUser is not null)
		{
			// Check if Pro user has completed onboarding
			if (foundUser.ProUser.OnboardingFormStop != "completed")
			{
				logger.LogWarning($"Pro user {foundUser.UserID} attempted to login with incomplete onboarding. OnboardingFormStop: {foundUser.ProUser.OnboardingFormStop}");
				return Result<AuthenticateUserResponseDto>.Failure(ErrorMessages.OnboardingIncomplete, ResultErrorType.BadRequest);
			}

			var proAuthDto = new AuthenticateProUserResponseDto(foundUser.UserID, response.AccessToken, response.RefreshToken, foundUser.ProUser.OnboardingFormStop);

			return Result<AuthenticateUserResponseDto>.Success(proAuthDto);
		}

		if (foundUser.EmployeeUser is not null)
		{
			var mapper = new EmployeeUserMapper();

			var employeeAuthDto = new AuthenticateEmployeeUserResponseDto(foundUser.UserID, response.AccessToken, response.RefreshToken, mapper.MapToEmployeeUserDetails(foundUser));

			return Result<AuthenticateUserResponseDto>.Success(employeeAuthDto);
		}

		var authDto = new AuthenticateUserResponseDto(foundUser.UserID, response.AccessToken, response.RefreshToken);

		return Result<AuthenticateUserResponseDto>.Success(authDto);
	}

	public async Task<Result<AuthenticateUserResponseDto>> RefreshAccessToken(RefreshTokenRequestDto refreshTokenRequest, CancellationToken ct)
	{
		var validationResult = await refreshValidator.ValidateAsync(refreshTokenRequest, ct);

		if (!validationResult.IsValid)
		{
			var errorMessage = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
			return Result<AuthenticateUserResponseDto>.Failure(errorMessage);
		}

		var tokenResponse = await identityRepository.RefreshAccessToken(refreshTokenRequest.RefreshToken, ct);


		if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
		{
			return Result<AuthenticateUserResponseDto>.Failure("Authentication failed");
		}

		var email = identityRepository.GetEmailFromAccessToken(tokenResponse.AccessToken);

		if (string.IsNullOrEmpty(email))
		{
			return Result<AuthenticateUserResponseDto>.Failure("Unable to find associated user");
		}

		var userInfo = await identityService.GetUserProfileByEmail(email, ct);

		if (userInfo == null || userInfo.IsDeleted)
		{
			return Result<AuthenticateUserResponseDto>.Failure("User is deleted");
		}

		if (userInfo.ProUser is not null)
		{
			var proResponse = new AuthenticateProUserResponseDto(
				userInfo.UserID,
				tokenResponse.AccessToken,
				tokenResponse.RefreshToken,
				userInfo.ProUser.OnboardingFormStop
			);

			return Result<AuthenticateUserResponseDto>.Success(proResponse);
		}
		else if (userInfo.EmployeeUser is not null)
		{
			var mapper = new EmployeeUserMapper();

			var employeeResponse = new AuthenticateEmployeeUserResponseDto(
					userInfo.UserID,
					tokenResponse.AccessToken,
					tokenResponse.RefreshToken,
					mapper.MapToEmployeeUserDetails(userInfo)
			);

			return Result<AuthenticateUserResponseDto>.Success(employeeResponse);
		}

		var response = new AuthenticateUserResponseDto(userInfo.UserID, tokenResponse.AccessToken, tokenResponse.RefreshToken);
		return Result<AuthenticateUserResponseDto>.Success(response);
	}

	public async Task<Result> ForgotPassword(string email, CancellationToken ct)
	{
		FlowUser? user = await identityService.GetUserProfileByEmail(email, ct);

		if (user == null || user.IsDeleted)
		{
			logger.LogWarning($"Password reset requested for non-existent or deleted email: {email}");

			// Return success to avoid revealing whether the email exists or not (security best practice)
			return Result.Success();
		}

		if (!user.UserID.HasValue)
		{
			logger.LogError($"User found but UserID is null for email: {email}");

			// Return success to avoid revealing whether the email exists or not (security best practice)
			return Result.Success();
		}

		Result<string> nonceResult = await nonceService.GenerateAndSaveNonce(user.UserID.Value, NonceType.PasswordReset, ct);

		if (!nonceResult.IsSuccess)
		{
			logger.LogError($"Failed to generate nonce for password reset for userID: {user.UserID}. Error: {nonceResult.Error}");

			// Return success to avoid revealing whether the email exists or not (security best practice)
			return Result.Success();
		}

		// send reset password email that includes link with nonce
		string resetLink = $"{_webAppBaseUrl}/reset-password?token={nonceResult.Value!.ToUrlEncoded()}";
		var emailResult = await emailSenderService.SendEmailPasswordResetLink(user, resetLink, ct);

		if (!emailResult.IsSuccess)
		{
			logger.LogError($"Failed to send password reset email to {user.Email}. Error: {emailResult.Error}");
		}

		// Return success to avoid revealing whether the email exists or not (security best practice)
		return Result.Success();
	}

	public async Task<Result> ResetPassword(ResetPasswordRequestDto request, CancellationToken ct)
	{
		try
		{
			Result result = await resetPasswordValidator.ValidateWithResult(request, ct)
				.MapResult(async (validRequest, ct) => await nonceService.ValidateAndConsumeNonce(validRequest.Token, ct), ct)
				.MapResult(GetUserForPasswordReset, ct)
				.MapResult(async (user, ct) => await UpdateUserPassword(user, request.NewPassword, ct), ct);

			return result;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Unexpected error during password reset");
			return Result.Failure("Password reset failed. Please try again.");
		}
	}

	private async Task<Result<FlowUser>> GetUserForPasswordReset(Nonce nonce, CancellationToken ct)
	{
		int userId = nonce.EntityId;

		var user = await repository.Query<FlowUser>()
			.ByUserID(userId)
			.ByIsDeleted(false)
			.AsNoTracking()
			.FirstOrDefaultAsync(ct);

		if (user == null || user.IsDeleted)
		{
			logger.LogError("User not found for valid nonce - data inconsistency. UserID: {UserId}", userId);
			return Result<FlowUser>.Failure("User not found");
		}

		if (user.IdentityGuid == Guid.Empty)
		{
			logger.LogError("User found but IdentityGuid is empty. UserID: {UserId}", userId);
			return Result<FlowUser>.Failure("Unable to reset password");
		}

		return Result<FlowUser>.Success(user);
	}

	private async Task<Result> UpdateUserPassword(FlowUser user, string newPassword, CancellationToken ct)
	{
		await identityRepository.SetUserPassword(user.IdentityGuid.ToString(), newPassword, ct);
		logger.LogInformation("Password successfully reset for user {UserId}", user.UserID);
		return Result.Success();
	}

	/// <summary>
	/// Authenticates a user via a magic link using a nonce.
	/// </summary>
	/// <param name="request">The magic link request containing the nonce.</param>
	/// <param name="ct">Cancellation token for the async operation.</param>
	/// <returns>A result containing the authentication response DTO.</returns>
	public async Task<Result<AuthenticateUserResponseDto>> AuthenticateViaMagicLink(MagicLinkDto request, CancellationToken ct)
	{
		var result = await magicLinkValidator.ValidateWithResult(request, ct)
			.MapResult(async (validRequest, ct) => await nonceService.ValidateAndConsumeNonce(validRequest.Nonce, ct), ct)
			.MapResult(async (nonce, ct) =>
			{
				Result<string> customTokenResult = nonce.Type switch
				{
					NonceType.CustomerMagicLink => await GenerateCustomJwtForCustomer(nonce.EntityId, ct),
					NonceType.EstimateRecipientMagicLink => await GenerateCustomJwtForEstimateRecipient(nonce.EntityId),
					_ => Result<string>.Failure("Unsupported nonce type."),
				};

				return customTokenResult;
			}, ct)
			.MapResult((token) =>
			{
				var authResponse = new AuthenticateUserResponseDto(null, token, null);
				return Result<AuthenticateUserResponseDto>.Success(authResponse);
			}, ct);

		return result;
	}

	private async Task<Result<string>> GenerateCustomJwtForEstimateRecipient(int recipientId)
	{
		EstimateRecipient? recipient = await repository.Query<EstimateRecipient>()
			.Where(er => er.EstimateRecipientId == recipientId)
			.FirstOrDefaultAsync();

		if (recipient == null)
			return Result<string>.Failure("Estimate recipient not found.");

		var claims = new[] {
			new Claim(CustomClaimTypes.ExternalId, recipient.EstimateRecipientId.ToString()),
			new Claim(CustomClaimTypes.EstimateId, recipient.EstimateId.ToString()),
			new Claim(CustomClaimTypes.PreferredUsername, recipient.RecipientEmail),
			new Claim(ClaimTypes.Role, nameof(Roles.EstimateRecipient))
		};

		Result<string> result = customJwtService.GenerateCustomJwt(claims, _customJwtConfig.MagicLinkTokenExpirationMinutes);

		return result;
	}

	private async Task<Result<string>> GenerateCustomJwtForCustomer(int customerId, CancellationToken ct)
	{
		FlowUser? customer = await repository.Query<FlowUser>()
			.Where(u => u.UserID == customerId)
			.FirstOrDefaultAsync(ct);

		if (customer == null)
			return Result<string>.Failure("Customer not found.");

		if (!customer.UserID.HasValue)
			return Result<string>.Failure("Customer UserID is null.");

		// Reactivate deactivated users when they use a magic link
		if (customer.IsDeleted)
		{
			customer.IsDeleted = false;
			customer.DateDeleted = null;
			customer.DateUpdated = DateTimeOffset.UtcNow;
			repository.AddOrUpdate(customer);
			await repository.SaveAsync(ct);

			logger.LogInformation("Reactivated customer {UserID} ({Email}) via magic link authentication",
				customer.UserID, customer.Email);
		}

		var claims = new[] {
			new Claim(CustomClaimTypes.ExternalId, customer.UserID.Value.ToString()),
			new Claim(CustomClaimTypes.PreferredUsername, customer.Email),
			new Claim(ClaimTypes.Role, nameof(Roles.Customer))
		};

		Result<string> result = customJwtService.GenerateCustomJwt(claims, _customJwtConfig.MagicLinkTokenExpirationMinutes);

		return result;
	}

	public async Task<Result<TokenResponseDto>> SavePassword(SavePasswordRequestDto info, CancellationToken ct)
	{
		var result = await savePasswordValidator.ValidateWithResult(info, ct)
			.MapResult(async (validRequest, ct) =>
			{
				int userId;

				try
				{
					userId = currentUserService.GetUserID();
				}
				catch (UnauthorizedAccessException ex)
				{
					logger.LogError(ex, "Unauthorized access when attempting to save password.");
					return Result<TokenResponseDto>.Failure("Unauthorized access. Please log in and try again.");
				}

				return await identityService.SaveUserPassword(userId, info.Password, ct);
			}, ct);

		return result;
	}
}
