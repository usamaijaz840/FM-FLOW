using FMFlow.Admin.Interface.DTOs;
using FMFlow.FlowAPI.Interface;

namespace FMFlow.Admin.Interface;

public interface ILeadSourcesService
{
	IEnumerable<LeadSourceResponseDto> GetLeadSources(bool includeDeleted, CancellationToken ct);

	Task<Result<LeadSourceResponseDto>> CreateLeadSource(LeadSourceRequestDto request, CancellationToken ct);

	Task<Result<LeadSourceResponseDto>> UpdateLeadSource(int leadSourceId, LeadSourceRequestDto request, CancellationToken ct);

	Task<Result> UpdateLeadSourceDeletedStatus(int leadSourceId, bool isDeleted, CancellationToken ct);
}
