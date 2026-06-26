using FMFlow.Common;
using FMFlow.Entities;
using FMFlow.Estimates.Interface.DTOs;
using FMFlow.FlowAPI.Interface;

namespace FMFlow.Estimates.Interface;

public interface IJobsService
{
	Task<Result<JobResponseDto>> CreateJob(JobRequestDto request, CancellationToken ct);

	Task<Result<DetailedJobResponseDto>> GetJob(int jobId, CancellationToken ct);

	Task<Result<JobResponseDto>> UpdateJob(int jobId, JobUpdateRequestDto request, CancellationToken ct);

	Task<Result<Dictionary<string, SearchResult<KanbanEstimateOrJobResponseDto>>>> SearchJobsForKanban(KanbanJobStatus? jobStatus, int? proUserId, int pageIndex, int pageSize, CancellationToken ct);

	Task<Result<JobSignOffResponseDto>> CreateJobSignOff(int jobId, JobSignOffRequestDto request, CancellationToken ct);
}
