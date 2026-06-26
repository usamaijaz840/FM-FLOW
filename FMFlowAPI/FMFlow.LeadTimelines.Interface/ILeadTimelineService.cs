using FMFlow.Common;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.LeadTimelines.Interface.DTOs;

namespace FMFlow.LeadTimelines.Interface;

public interface ILeadTimelineService
{
	Task<Result<SearchResult<LeadTimelineResponseDto>>> GetLeadTimeline(int leadId, int pageIndex, int pageSize, CancellationToken ct);

	Task<Result> RecordLeadTimelineAsync(Estimate estimate, TimelineEventKey eventKey, CancellationToken ct, string? statusUpdateReason = null);

	Task<Result> RecordLeadTimelineAsync(Job job, TimelineEventKey eventKey, CancellationToken ct, string? statusUpdateReason = null);

	Task<Result> RecordLeadTimelineAsync(Lead lead, TimelineEventKey eventKey, CancellationToken ct);

	Task<Result> RecordLeadTimelineAsync(Lead lead, TimelineEventKey eventKey, int userId, CancellationToken ct);
}
