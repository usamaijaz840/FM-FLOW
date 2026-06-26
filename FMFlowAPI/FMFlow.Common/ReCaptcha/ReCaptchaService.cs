using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FMFlow.Common.ReCaptcha;

public partial class ReCaptchaService : IReCaptchaService
{
	private readonly HttpClient HttpClient;
	private readonly ILogger<ReCaptchaService> Logger;
	private readonly string SecretKey;
	private const string GoogleReCaptchaVerifyUrl = "https://www.google.com/recaptcha/api/siteverify";

	public ReCaptchaService(HttpClient httpClient, IConfiguration configuration, ILogger<ReCaptchaService> logger)
	{
		HttpClient = httpClient;
		SecretKey = configuration["ReCaptcha:SecretKey"] ?? throw new ArgumentNullException("ReCaptcha:SecretKey", "reCAPTCHA secret key is not configured in app settings");
		Logger = logger;
	}

	public async Task<bool> VerifyTokenIsValid(string token, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(token))
		{
			Logger.LogWarning("reCAPTCHA token is null or empty");
			throw new ArgumentException("reCAPTCHA token is required");
		}

		
		var formData = new Dictionary<string, string>
		{
			{"secret", SecretKey},
			{"response", token}
		};

		var formContent = new FormUrlEncodedContent(formData);
		var response = await HttpClient.PostAsync(GoogleReCaptchaVerifyUrl, formContent, ct);

		if (response.IsSuccessStatusCode is false)
		{
			Logger.LogError("Failed to verify reCAPTCHA token. HTTP Status: {StatusCode}", response.StatusCode);
			return false;
		}

		var responseContent = await response.Content.ReadAsStringAsync(ct);
		var verificationResult = JsonSerializer.Deserialize<ReCaptchaVerificationResponse>(responseContent);

		if (verificationResult?.Success == true)
		{
			Logger.LogInformation("reCAPTCHA verification successful");
			return true;
		}

		var errorCodes = verificationResult?.ErrorCodes != null
			? string.Join(", ", verificationResult.ErrorCodes)
			: "Unknown error";

		Logger.LogWarning("reCAPTCHA verification failed. Error codes: {ErrorCodes}", string.Join(", ", errorCodes));
		return false;
	}
}
