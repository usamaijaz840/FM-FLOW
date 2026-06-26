using System.Security.Claims;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Identity.Interface;
using Microsoft.Extensions.Options;

namespace FMFlow.Identity.Service;

public class OnboardingTokenService(ICustomJwtService customJwtService, IOptions<CustomJwtConfiguration> customJwtOptions)
	: IOnboardingTokenService
{
	private readonly ICustomJwtService _customJwtService = customJwtService;
	private readonly CustomJwtConfiguration _customJwtConfig = customJwtOptions.Value;

	public Result<string> GenerateInitialOnboardingToken()
	{
		// Onboarding token for initial pages; valid for both Pros and Places onboarding
		var claims = new List<Claim> {
			new(CustomClaimTypes.TokenPurpose, "onboarding"),
		};

		// Configurable short lifetime
		return _customJwtService.GenerateCustomJwt(claims, expiresMinutes: _customJwtConfig.InitialOnboardingTokenExpirationMinutes);
	}
}
