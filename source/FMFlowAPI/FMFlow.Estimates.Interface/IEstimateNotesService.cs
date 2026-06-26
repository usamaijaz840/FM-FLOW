using FMFlow.Common;
using FMFlow.Estimates.Interface.DTOs;
using FMFlow.FlowAPI.Interface;

namespace FMFlow.Estimates.Interface;

public interface IEstimateNotesService
{
    Task<Result<SearchResult<EstimateNoteResponseDto>>> GetEstimateNotes(int estimateId, int pageIndex, int pageSize, CancellationToken ct);
    
    Task<Result<EstimateNoteResponseDto>> CreateEstimateNote(int estimateId, EstimateNoteRequestDto createNoteRequest, CancellationToken ct);
    
    Task<Result<EstimateNoteResponseDto>> UpdateEstimateNote(int estimateId, int noteId, EstimateNoteRequestDto updateNoteRequest, CancellationToken ct);
    
    Task<Result> DeleteEstimateNote(int estimateId, int noteId, CancellationToken ct);
} 