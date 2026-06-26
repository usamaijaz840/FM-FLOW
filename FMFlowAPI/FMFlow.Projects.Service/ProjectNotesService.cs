using FMFlow.Common;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Projects.Interface;
using FMFlow.Projects.Interface.DTOs;
using FMFlow.Projects.Service.Mappers;
using FMFlow.Projects.Service.Validators;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using EFRepository;

namespace FMFlow.Projects.Service;

public class ProjectNotesService(
	IRepository repository,
	IValidator<ProjectNoteRequestDto> validator)
	: IProjectNotesService
{
	public async Task<Result<SearchResult<ProjectNoteResponseDto>>> GetProjectNotes(int projectId, int userId, bool isPro, bool isAccountManagerOrAdmin,
		int pageIndex, int pageSize, CancellationToken ct)
	{
		// Verify project exists
		var projectExists = await repository
			.Query<Project>()
			.Where(p => p.ProjectID == projectId)
			.Where(p => !p.IsDeleted)
			.AnyAsync(ct);

		if (!projectExists)
			return Result<SearchResult<ProjectNoteResponseDto>>.Failure("Project not found", ResultErrorType.NotFound);

		// Build query based on user role
		IQueryable<ProjectNote> query = repository
			.Query<ProjectNote>()
			.Where(n => n.ProjectId == projectId)
			.Where(n => !n.IsDeleted);

		// If user is a Pro, only show notes created by them
		if (isPro)
		{
			query = query.Where(n => n.ProId == userId);
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
		var mapper = new ProjectNoteMapper();

		var noteDtos = notes.Select(n => mapper.MapToProjectNoteResponseDto(n)).ToList();

		// Create SearchResult
		var result = new SearchResult<ProjectNoteResponseDto>(
			noteDtos,
			totalCount
		);

		return Result<SearchResult<ProjectNoteResponseDto>>.Success(result);
	}

	public async Task<Result<ProjectNoteResponseDto>> CreateProjectNote(int projectId, ProjectNoteRequestDto createNoteRequest, int? proId,
		CancellationToken ct)
	{
		// Validate request
		var validationResult = await validator.ValidateAsync(createNoteRequest, ct);
		if (!validationResult.IsValid)
			return Result<ProjectNoteResponseDto>.Failure(validationResult.ToString(), ResultErrorType.BadRequest);

		// Verify project exists
		var projectExists = await repository
			.Query<Project>()
			.Where(p => p.ProjectID == projectId)
			.Where(p => !p.IsDeleted)
			.AnyAsync(ct);

		if (!projectExists)
			return Result<ProjectNoteResponseDto>.Failure("Project not found", ResultErrorType.NotFound);

		// Create the note using the mapper
		var mapper = new ProjectNoteMapper();
		var newNote = new ProjectNote();
		mapper.UpdateProjectNote(createNoteRequest, newNote);

		// Set additional properties not covered by the mapper
		newNote.ProjectId = projectId;
		newNote.ProId = proId;
		newNote.DateCreated = DateTimeOffset.UtcNow;

		repository.AddNew(newNote);
		await repository.SaveAsync(ct);

		// Return the created note using mapper
		var responseDto = mapper.MapToProjectNoteResponseDto(newNote);

		return Result<ProjectNoteResponseDto>.Success(responseDto);
	}

	public async Task<Result<ProjectNoteResponseDto>> UpdateProjectNote(int projectId, int noteId, ProjectNoteRequestDto updateNoteRequest,
		int userId, bool isPro, bool isAccountManagerOrAdmin, CancellationToken ct)
	{
		// Validate request
		var validationResult = await validator.ValidateAsync(updateNoteRequest, ct);
		if (!validationResult.IsValid)
			return Result<ProjectNoteResponseDto>.Failure(validationResult.ToString(), ResultErrorType.BadRequest);

		// Get the note
		var note = await repository
			.Query<ProjectNote>()
			.Where(n => n.ProjectNoteId == noteId)
			.Where(n => n.ProjectId == projectId)
			.Where(n => !n.IsDeleted)
			.FirstOrDefaultAsync(ct);

		if (note == null)
			return Result<ProjectNoteResponseDto>.Failure("Note not found", ResultErrorType.NotFound);

		// Check permissions
		if (isPro && note.ProId != userId)
			return Result<ProjectNoteResponseDto>.Failure("Access denied", ResultErrorType.BadRequest);

		// Update the note using mapper
		var mapper = new ProjectNoteMapper();

		mapper.UpdateProjectNote(updateNoteRequest, note);
		note.DateUpdated = DateTimeOffset.UtcNow;

		repository.AddOrUpdate(note);
		await repository.SaveAsync(ct);

		// Return the updated note using mapper
		var responseDto = mapper.MapToProjectNoteResponseDto(note);

		return Result<ProjectNoteResponseDto>.Success(responseDto);
	}

	public async Task<Result> DeleteProjectNote(int projectId, int noteId, int userId, bool isPro, bool isAccountManagerOrAdmin,
		CancellationToken ct)
	{
		// Get the note
		var note = await repository
			.Query<ProjectNote>()
			.Where(n => n.ProjectNoteId == noteId)
			.Where(n => n.ProjectId == projectId)
			.Where(n => !n.IsDeleted)
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
