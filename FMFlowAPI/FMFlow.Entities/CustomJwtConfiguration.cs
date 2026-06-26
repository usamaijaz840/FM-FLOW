namespace FMFlow.Entities;

public class CustomJwtConfiguration
{
	/// <summary>
	/// JWT Issuer (iss claim)
	/// </summary>
	public required string Issuer { get; set; }

	/// <summary>
	/// JWT Audience (aud claim)
	/// </summary>
	public required string Audience { get; set; }

	/// <summary>
	/// Signing key for HS256 (symmetric algorithm)
	/// Minimum 256 bits (32 bytes) recommended
	/// </summary>
	public required string SigningKey { get; set; }

	/// <summary>
	/// Token expiration time in minutes for magic link authentication
	/// </summary>
	public int MagicLinkTokenExpirationMinutes { get; set; }

	/// <summary>
	/// Token expiration time in minutes for initial onboarding tokens
	/// </summary>
	public int InitialOnboardingTokenExpirationMinutes { get; set; }

	/// <summary>
	/// Token expiration time in minutes for Pro onboarding custom JWTs (used when creating a new 
	/// Pro via CreateProWithToken) and before they have set a password
	/// </summary>
	public int ProOnboardingTokenExpirationMinutes { get; set; }

	/// <summary>
	/// Token expiration time in minutes for Customer onboarding custom JWTs (used when creating a new
	/// Customer via CreateCustomerWithToken) and before they have set a password
	/// </summary>
	public int CustomerOnboardingTokenExpirationMinutes { get; set; }
}
