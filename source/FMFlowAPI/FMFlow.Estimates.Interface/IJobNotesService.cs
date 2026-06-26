using FMFlow.Common;
using FMFlow.Estimates.Interface.DTOs;
using FMFlow.FlowAPI.Interface;

namespace FMFlow.Estimates.Interface;

public interface IJobNotesService
{
	Task<Result<SearchResult<JobNoteResponseDto>>> GetJobNotes(int jobId, int pageIndex, int pageSize, CancellationToken ct);

	Task<Result<JobNoteResponseDto>> CreateJobNote(int jobId, JobNoteRequestDto createNoteRequest, CancellationToken ct);

	Task<Result<JobNoteResponseDto>> UpdateJobNote(int jobId, int noteId, JobNoteRequestDto updateNoteRequest, CancellationToken ct);

	Task<Result> DeleteJobNote(int jobId, int noteId, CancellationToken ct);
}
