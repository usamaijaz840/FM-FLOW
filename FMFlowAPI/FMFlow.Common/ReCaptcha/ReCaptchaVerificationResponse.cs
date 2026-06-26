using System.Text.Json.Serialization;

namespace FMFlow.Common.ReCaptcha;

public partial class ReCaptchaService
{
	private record ReCaptchaVerificationResponse
	{
		[JsonPropertyName("success")]
		public bool Success { get; set; }
		[JsonPropertyName("challenge_ts")]
		public string? ChallengeTs { get; set; }
		[JsonPropertyName("hostname")]
		public string? Hostname { get; set; }
		[JsonPropertyName("error-codes")]
		public string[]? ErrorCodes { get; set; }
	}
}
