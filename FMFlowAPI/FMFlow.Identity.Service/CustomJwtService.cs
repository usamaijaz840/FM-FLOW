using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Identity.Interface;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FMFlow.Identity.Service;

public class CustomJwtService(
	IOptions<CustomJwtConfiguration> options,
	SecurityTokenHandler _tokenHandler) : ICustomJwtService
{
	private readonly CustomJwtConfiguration _options = options.Value;
	private readonly Lazy<TokenValidationParameters> _validationParameters = new(() => 
		CreateValidationParameters(options.Value));

	public Result<string> GenerateCustomJwt(IEnumerable<Claim> claims, int expiresMinutes)
	{
		try
		{
			var claimsList = claims.ToList();
			var now = DateTime.UtcNow;

			// Add JWT ID (jti) for token tracking and per-token rate limiting
			claimsList.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")));

			// Add issued at time (iat)
			claimsList.Add(new Claim(JwtRegisteredClaimNames.Iat,
				new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
				ClaimValueTypes.Integer64));

			// Extract role claims to add to realm_access
			var roleClaims = claimsList
				.Where(c => c.Type == ClaimTypes.Role)
				.Select(c => c.Value.ToLowerInvariant())
				.ToList();

			// Create realm_access claim in Keycloak format for frontend compatibility
			var realmAccess = new { roles = roleClaims };
			string realmAccessJson = JsonSerializer.Serialize(realmAccess);

			claimsList.Add(new Claim("realm_access", realmAccessJson, JsonClaimValueTypes.Json));

			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			DateTime expires = now.AddMinutes(expiresMinutes);

			var token = new JwtSecurityToken(
				issuer: _options.Issuer,
				audience: _options.Audience,
				claims: claimsList,
				expires: expires,
				signingCredentials: creds
			);

			string tokenString = _tokenHandler.WriteToken(token);

			return Result<string>.Success(tokenString);

		}
		catch (Exception ex)
		{
			return Result<string>.Failure($"Failed to generate JWT token: {ex.Message}");
		}
	}

	public static TokenValidationParameters CreateValidationParameters(CustomJwtConfiguration config)
	{
		var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.SigningKey));

		return new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidIssuer = config.Issuer,
			ValidateAudience = true,
			ValidAudience = config.Audience,
			ValidateLifetime = true,
			IssuerSigningKey = signingKey,
			ValidateIssuerSigningKey = true,
			RequireSignedTokens = true,
			RequireExpirationTime = true,
			ClockSkew = TimeSpan.Zero,
			ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
		};
	}

	public Result<ClaimsPrincipal> ValidateCustomJwt(string token)
	{
		if (string.IsNullOrWhiteSpace(token))
			return Result<ClaimsPrincipal>.Failure("Attempted to validate null or empty JWT token");

		try
		{
			ClaimsPrincipal principal = _tokenHandler.ValidateToken(token, _validationParameters.Value, out var validatedToken);
			return Result<ClaimsPrincipal>.Success(principal);
		}
		catch (Exception ex)
		{
			return Result<ClaimsPrincipal>.Failure($"Failed to validate JWT token: {ex.Message}");
		}
	}
}
