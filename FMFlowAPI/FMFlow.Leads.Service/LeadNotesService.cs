using FMFlow.Common;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Leads.Interface;
using FMFlow.Leads.Interface.DTOs;
using FMFlow.Leads.Service.Mappers;
using FMFlow.Leads.Service.Validators;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using EFRepository;

namespace FMFlow.Leads.Service;

public class LeadNotesService(
	IRepository repository,
	IValidator<LeadNoteRequestDto> validator)
	: ILeadNotesService
{
	public async Task<Result<SearchResult<LeadNoteResponseDto>>> GetLeadNotes(int leadId, int userId, bool isPro, bool isAccountManagerOrAdmin,
		int pageIndex, int pageSize, CancellationToken ct)
	{
		// Verify lead exists
		var leadExists = await repository
			.Query<Lead>()
			.ByLeadID(leadId)
			.ByIsDeleted(false)
			.AnyAsync(ct);

		if (!leadExists)
			return Result<SearchResult<LeadNoteResponseDto>>.Failure("Lead not found", ResultErrorType.NotFound);

		// Build query based on user role
		IQueryable<LeadNote> query = repository
			.Query<LeadNote>()
			.ByLeadId(leadId)
			.ByIsDeleted(false);

		// If user is a Pro, only show notes created by them
		if (isPro)
		{
			query = query.ByProId(userId);
		}
		// Otherwise (AccountManager or SuperAdmin), show all notes

		// Get total count
		var totalCount = await query.CountAsync(ct);

		// Apply pagination
		var notes = await query
			.OrderByDescending(n => n.DateCreated)
			.Skip(pageIndex * pageSize)
			.Take(pageSize)
			.ToListAsync(ct);

		// Map to DTOs using mapper
		var mapper = new LeadNoteMapper();

		var noteDtos = notes.Select(n => mapper.MapToLeadNoteResponseDto(n)).ToList();

		// Check the constructor pattern used in the codebase
		var result = new SearchResult<LeadNoteResponseDto>(
			noteDtos,
			totalCount
		);

		return Result<SearchResult<LeadNoteResponseDto>>.Success(result);
	}

	public async Task<Result<LeadNoteResponseDto>> CreateLeadNote(int leadId, LeadNoteRequestDto createNoteRequest, int? proId,
		CancellationToken ct)
	{
		// Validate request
		var validationResult = await validator.ValidateAsync(createNoteRequest, ct);
		if (!validationResult.IsValid)
			return Result<LeadNoteResponseDto>.Failure(validationResult.ToString(), ResultErrorType.BadRequest);

		// Verify lead exists
		var leadExists = await repository
			.Query<Lead>()
			.ByLeadID(leadId)
			.ByIsDeleted(false)
			.AnyAsync(ct);

		if (!leadExists)
			return Result<LeadNoteResponseDto>.Failure("Lead not found", ResultErrorType.NotFound);

		// Create the note using the mapper
		var mapper = new LeadNoteMapper();
		var newNote = new LeadNote();
		mapper.UpdateLeadNote(createNoteRequest, newNote);

		// Set additional properties not covered by the mapper
		newNote.LeadId = leadId;
		newNote.ProId = proId;
		newNote.DateCreated = DateTimeOffset.UtcNow;

		repository.AddNew(newNote);
		await repository.SaveAsync(ct);

		// Return the created note using mapper
		var responseDto = mapper.MapToLeadNoteResponseDto(newNote);

		return Result<LeadNoteResponseDto>.Success(responseDto);
	}

	public async Task<Result<LeadNoteResponseDto>> UpdateLeadNote(int leadId, int noteId, LeadNoteRequestDto updateNoteRequest,
		int userId, bool isPro, bool isAccountManagerOrAdmin, CancellationToken ct)
	{
		// Validate request
		var validationResult = await validator.ValidateAsync(updateNoteRequest, ct);
		if (!validationResult.IsValid)
			return Result<LeadNoteResponseDto>.Failure(validationResult.ToString(), ResultErrorType.BadRequest);

		// Get the note
		var note = await repository
			.Query<LeadNote>()
			.ByLeadNoteId(noteId)
			.ByLeadId(leadId)
			.ByIsDeleted(false)
			.FirstOrDefaultAsync(ct);

		if (note == null)
			return Result<LeadNoteResponseDto>.Failure("Note not found", ResultErrorType.NotFound);

		// Check permissions
		if (isPro && note.ProId != userId)
			return Result<LeadNoteResponseDto>.Failure("Access denied", ResultErrorType.BadRequest);

		// Update the note using mapper
		var mapper = new LeadNoteMapper();

		mapper.UpdateLeadNote(updateNoteRequest, note);
		note.DateUpdated = DateTimeOffset.UtcNow;

		repository.AddOrUpdate(note);
		await repository.SaveAsync(ct);

		// Return the updated note using mapper
		var responseDto = mapper.MapToLeadNoteResponseDto(note);

		return Result<LeadNoteResponseDto>.Success(responseDto);
	}

	public async Task<Result> DeleteLeadNote(int leadId, int noteId, int userId, bool isPro, bool isAccountManagerOrAdmin,
		CancellationToken ct)
	{
		// Get the note
		var note = await repository
			.Query<LeadNote>()
			.ByLeadNoteId(noteId)
			.ByLeadId(leadId)
			.ByIsDeleted(false)
			.FirstOrDefaultAsync(ct);

		if (note == null)
			return Result.Failure("Note not found", ResultErrorType.NotFound);

		// Check permissions
		if (isPro && note.ProId != userId)
			return Result.Failure("Access denied", ResultErrorType.BadRequest);

		// Soft delete the note
		note.IsDeleted = true;
		note.DateDeleted = DateTimeOffset.UtcNow;

		repository.AddOrUpdate(note);
		await repository.SaveAsync(ct);

		return Result.Success();
	}
}
