using FMFlow.Common;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Leads.Interface.DTOs;

namespace FMFlow.Leads.Interface;

public interface ILeadNotesService
{
    Task<Result<SearchResult<LeadNoteResponseDto>>> GetLeadNotes(int leadId, int userId, bool isPro, bool isAccountManagerOrAdmin, int pageIndex, int pageSize, CancellationToken ct);
    
    Task<Result<LeadNoteResponseDto>> CreateLeadNote(int leadId, LeadNoteRequestDto createNoteRequest, int? proId, CancellationToken ct);
    
    Task<Result<LeadNoteResponseDto>> UpdateLeadNote(int leadId, int noteId, LeadNoteRequestDto updateNoteRequest, int userId, bool isPro, bool isAccountManagerOrAdmin, CancellationToken ct);
    
    Task<Result> DeleteLeadNote(int leadId, int noteId, int userId, bool isPro, bool isAccountManagerOrAdmin, CancellationToken ct);
} 