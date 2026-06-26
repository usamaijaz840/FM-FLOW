using System.Security.Claims;
using FMFlow.FlowAPI.Interface;

namespace FMFlow.Identity.Interface;

public interface ICustomJwtService
{
	Result<string> GenerateCustomJwt(IEnumerable<Claim> claims, int expiresMinutes);

	Result<ClaimsPrincipal> ValidateCustomJwt(string token);
}
