using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;

namespace FMFlow.AccessValidation;

public interface IAccessValidator
{
	Task<Result> ValidateAccessToProject(int projectId, CancellationToken ct);

	Task<Result> ValidateAccessToProject(Project project, CancellationToken ct);

	Task<Result> ValidateAccessToProject(ScheduledEstimate scheduledEstimate, CancellationToken ct);

	Task<Result> ValidateAccessToRequestedEstimate(int requestedEstimateId, CancellationToken ct);

	Task<Result> ValidateAccessToRequestedEstimate(RequestedEstimate requestedEstimate, CancellationToken ct);

	Task<Result> ValidateAccessToEstimate(int estimateId, CancellationToken ct);

	Task<Result> ValidateAccessToEstimate(Estimate estimate, CancellationToken ct);

	Task<Result> ValidateAccessToEstimateFile(FileItemToEstimate fileEstimate, CancellationToken ct);

	Task<Result> ValidateAccessToLead(Lead lead, CancellationToken ct);
}
