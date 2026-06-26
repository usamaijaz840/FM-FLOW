using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;

namespace FMFlow.Integrations.Interface;

/// <summary>
/// Service for refreshing OAuth access tokens
/// </summary>
public interface ITokenRefreshService
{
	/// <summary>
	/// Ensures the integration has a valid access token, refreshing if necessary
	/// </summary>
	/// <param name="integration">The integration to refresh</param>
	/// <param name="ct">Cancellation token</param>
	/// <returns>Updated integration with valid access token, or error if refresh fails</returns>
	Task<Result<Integration>> EnsureValidTokenAsync(Integration integration, CancellationToken ct);
}
