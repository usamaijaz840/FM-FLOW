using FMFlow.Common;
using FMFlow.Entities;
using FMFlow.Estimates.Interface.DTOs;
using FMFlow.FlowAPI.Interface;

namespace FMFlow.Estimates.Interface;

public interface IEstimatesService
{
	Task<Result<RequestedEstimateResponseDto>> CreateRequestedEstimate(RequestedEstimateRequestDto request, CancellationToken ct);

	Task<Result<EstimateResponseDto>> CreateEstimate(EstimateRequestDto request, CancellationToken ct);

	Task<Result<RequestedEstimateResponseDto>> GetRequestedEstimate(int requestedEstimateId, CancellationToken ct);

	Task<Result<DetailedEstimateResponseDto>> GetDetailedEstimate(int estimateId, CancellationToken ct);

	Task<Result<List<RequestedEstimateResponseDto>>> GetRequestedEstimatesByProjectID(int projectId, CancellationToken ct);

	Task<Result<RequestedEstimateResponseDto>> UpdateRequestedEstimate(int requestedEstimateId, RequestedEstimateUpdateRequestDto updateRequest, CancellationToken ct);

	Task<Result<DetailedEstimateResponseDto>> UpdateEstimate(int estimateId, EstimateUpdateRequestDto updateRequest, CancellationToken ct);

	Task<Result> DeleteRequestedEstimate(int requestedEstimateId, CancellationToken ct);

	Task<Result<SearchResult<SearchEstimatesResponseDto>>> SearchEstimates(int projectId, string? keywordSearch, CancellationToken ct);

	Task<Result<Dictionary<string, SearchResult<KanbanEstimateOrJobResponseDto>>>> SearchEstimatesForKanban(KanbanEstimateStatus? estimateStatus, int? proUserId, int pageIndex, int pageSize, CancellationToken ct);

	Task<Result> SendAdditionalEstimateFinalizedEmails(int estimateId, EstimateSendEmailsRequestDto request, CancellationToken ct);

	Task<Result> ResendEstimateReviewEmail(ResendEstimateReviewEmailRequestDto request, CancellationToken ct);
}
