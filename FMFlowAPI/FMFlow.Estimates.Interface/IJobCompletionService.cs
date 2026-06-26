namespace FMFlow.Estimates.Interface;

public interface IJobCompletionService
{
	Task CloseJobIfEligible(int jobId, CancellationToken ct);
}
