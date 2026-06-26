using FMFlow.FlowAPI.Interface;
using FMFlow.Identity.Interface.DTOs;
using FMFlow.Login.Interface.DTOs;

namespace FMFlow.Login.Interface;

public interface ILoginService
{
	Task<Result<AuthenticateUserResponseDto>> AuthenticateUser(AuthenticateUserRequestDto request, CancellationToken ct);

	Task<Result<AuthenticateUserResponseDto>> RefreshAccessToken(RefreshTokenRequestDto refreshToken, CancellationToken ct);

	/// <summary>
	/// Initiates the password reset process for the specified email address.
	/// <para>
	/// This method always returns success, regardless of whether the email is associated with a valid or deleted user,
	/// to prevent user enumeration attacks.
	/// </para>
	/// <para>
	/// Side effects:
	/// <list type="bullet">
	/// <item>If the email is associated with a valid user, a nonce is generated and a password reset email is sent.</item>
	/// <item>If the email is unknown or the user is deleted, no email is sent.</item>
	/// </list>
	/// </para>
	/// Consumers should not rely on the result to determine if the email exists in the system.
	/// </summary>
	Task<Result> ForgotPassword(string email, CancellationToken ct);

	Task<Result> ResetPassword(ResetPasswordRequestDto request, CancellationToken ct);

	Task<Result<AuthenticateUserResponseDto>> AuthenticateViaMagicLink(MagicLinkDto request, CancellationToken ct);

	Task<Result<TokenResponseDto>> SavePassword(SavePasswordRequestDto info, CancellationToken ct);
}
