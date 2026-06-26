using FMFlow.Common;
using FMFlow.Estimates.Interface.DTOs;
using FMFlow.FlowAPI.Interface;

namespace FMFlow.Estimates.Interface;

public interface IScheduledEstimatesService
{
	Task<Result<ScheduledEstimateResponseDto>> CreateScheduledEstimate(ScheduledEstimateRequestDto request, CancellationToken ct, bool isBatch = false);

	Task<Result<SearchResult<ScheduledEstimateResponseDto>>> SearchScheduledEstimates(
		int projectID,
		int pageIndex,
		int pageSize,
		CancellationToken ct);

	Task<Result<ScheduledEstimateResponseDto>> GetScheduledEstimate(int scheduledEstimateId, CancellationToken ct);

	Task<Result<ScheduledEstimateResponseDto>> UpdateScheduledEstimate(int scheduledEstimateId, ScheduledEstimateUpdateRequestDto request, CancellationToken ct);

	Task<Result> DeleteScheduledEstimate(int scheduledEstimateId, CancellationToken ct);
}
