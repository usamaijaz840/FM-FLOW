using FMFlow.Common;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Projects.Interface.DTOs;

namespace FMFlow.Projects.Interface;

public interface IProjectNotesService
{
    Task<Result<SearchResult<ProjectNoteResponseDto>>> GetProjectNotes(int projectId, int userId, bool isPro, bool isAccountManagerOrAdmin, int pageIndex, int pageSize, CancellationToken ct);
    
    Task<Result<ProjectNoteResponseDto>> CreateProjectNote(int projectId, ProjectNoteRequestDto createNoteRequest, int? proId, CancellationToken ct);
    
    Task<Result<ProjectNoteResponseDto>> UpdateProjectNote(int projectId, int noteId, ProjectNoteRequestDto updateNoteRequest, int userId, bool isPro, bool isAccountManagerOrAdmin, CancellationToken ct);
    
    Task<Result> DeleteProjectNote(int projectId, int noteId, int userId, bool isPro, bool isAccountManagerOrAdmin, CancellationToken ct);
} 
