using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FMFlow.Data.PostgresSeeder;

public sealed class SeedStatusHealthCheck(ISeedStatusService seedStatus) : IHealthCheck
{
	public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(
			seedStatus.IsComplete
				? HealthCheckResult.Healthy("Data seeding completed.")
				: HealthCheckResult.Unhealthy("Data seeding still in progress."));
	}
}
