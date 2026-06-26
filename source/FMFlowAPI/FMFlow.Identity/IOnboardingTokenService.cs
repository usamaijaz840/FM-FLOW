using FMFlow.FlowAPI.Interface;

namespace FMFlow.Identity.Interface;

public interface IOnboardingTokenService
{
	Result<string> GenerateInitialOnboardingToken();
}
