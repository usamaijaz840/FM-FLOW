using System.Threading.Tasks;

namespace FMFlow.Common.ReCaptcha;

public interface IReCaptchaService
{
	/// <summary>
	/// Verifies a reCAPTCHA token with Google's reCAPTCHA service
	/// </summary>
	/// <param name="token">The reCAPTCHA token from the client</param>
	/// <param name="ct">Cancellation token</param>
	/// <returns>Result indicating if the verification was successful</returns>
	Task<bool> VerifyTokenIsValid(string token, CancellationToken ct = default);
}
