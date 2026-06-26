using EFRepository;
using FluentValidation;
using FMFlow.AccessValidation;
using FMFlow.Common;
using FMFlow.Entities;
using FMFlow.Estimates.Interface;
using FMFlow.Estimates.Interface.DTOs;
using FMFlow.Estimates.Service.Mappers;
using FMFlow.FlowAPI.Interface;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.Estimates.Service;

public class EstimateNotesService(
	IRepository repository,
	IValidator<EstimateNoteRequestDto> validator,
	IAccessValidator accessValidator)
	: IEstimateNotesService
{
	public async Task<Result<SearchResult<EstimateNoteResponseDto>>> GetEstimateNotes(int estimateId, int pageIndex, int pageSize, CancellationToken ct)
	{
		var estimate = await repository
			.Query<Estimate>()
			.ByEstimateID(estimateId)
			.Include(e => e.RequestedEstimate)
					.ThenInclude(re => re.Project)
						.ThenInclude(p => p.Lead)
							.ThenInclude(l => l.Address)
			.FirstOrDefaultAsync(ct);

		if (estimate == null)
			return Result<SearchResult<EstimateNoteResponseDto>>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToEstimate(estimate, ct);

		if (!accessResult.IsSuccess)
			return Result<SearchResult<EstimateNoteResponseDto>>.Failure(accessResult.Error!, accessResult.ErrorType);

		var query = repository
			.Query<EstimateNote>()
			.ByEstimateId(estimateId)
			.ByIsDeleted(false);

		var totalCount = await query.CountAsync(ct);

		var notes = await query
			.OrderByDescending(n => n.DateCreated)
			.Skip(pageIndex * pageSize)
			.Take(pageSize)
			.ToListAsync(ct);

		var mapper = new EstimateNoteMapper();

		var noteDtos = notes.Select(mapper.MapToEstimateNoteResponseDto).ToList();

		var result = new SearchResult<EstimateNoteResponseDto>(noteDtos, totalCount);

		return Result<SearchResult<EstimateNoteResponseDto>>.Success(result);
	}

	public async Task<Result<EstimateNoteResponseDto>> CreateEstimateNote(int estimateId, EstimateNoteRequestDto createNoteRequest, CancellationToken ct)
	{
		var validationResult = await validator.ValidateAsync(createNoteRequest, ct);

		if (!validationResult.IsValid)
			return Result<EstimateNoteResponseDto>.Failure(validationResult.ToString(), ResultErrorType.BadRequest);

		var estimate = await repository
			.Query<Estimate>()
			.ByEstimateID(estimateId)
			.Include(e => e.RequestedEstimate)
				.ThenInclude(re => re.Project)
					.ThenInclude(p => p.Lead)
						.ThenInclude(l => l.Address)
			.FirstOrDefaultAsync(ct);

		if (estimate == null)
			return Result<EstimateNoteResponseDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToEstimate(estimate, ct);

		if (!accessResult.IsSuccess)
			return Result<EstimateNoteResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);

		var mapper = new EstimateNoteMapper();
		var newNote = new EstimateNote();
		mapper.UpdateEstimateNote(createNoteRequest, newNote);

		// Set additional properties not covered by the mapper
		newNote.EstimateId = estimateId;
		newNote.DateCreated = DateTimeOffset.UtcNow;

		repository.AddNew(newNote);
		await repository.SaveAsync(ct);

		var responseDto = mapper.MapToEstimateNoteResponseDto(newNote);

		return Result<EstimateNoteResponseDto>.Success(responseDto);
	}

	public async Task<Result<EstimateNoteResponseDto>> UpdateEstimateNote(int estimateId, int noteId, EstimateNoteRequestDto updateNoteRequest, CancellationToken ct)
	{
		var validationResult = await validator.ValidateAsync(updateNoteRequest, ct);

		if (!validationResult.IsValid)
			return Result<EstimateNoteResponseDto>.Failure(validationResult.ToString(), ResultErrorType.BadRequest);

		var estimate = await repository
			.Query<Estimate>()
			.ByEstimateID(estimateId)
			.Include(e => e.RequestedEstimate)
				.ThenInclude(re => re.Project)
					.ThenInclude(p => p.Lead)
						.ThenInclude(l => l.Address)
			.FirstOrDefaultAsync(ct);

		if (estimate == null)
			return Result<EstimateNoteResponseDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToEstimate(estimate, ct);

		if (!accessResult.IsSuccess)
			return Result<EstimateNoteResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);

		var note = await repository
			.Query<EstimateNote>()
			.ByEstimateId(estimateId)
			.ByEstimateNoteId(noteId)
			.ByIsDeleted(false)
			.FirstOrDefaultAsync(ct);

		if (note == null)
			return Result<EstimateNoteResponseDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var mapper = new EstimateNoteMapper();

		mapper.UpdateEstimateNote(updateNoteRequest, note);
		note.DateUpdated = DateTimeOffset.UtcNow;

		repository.AddOrUpdate(note);
		await repository.SaveAsync(ct);

		var responseDto = mapper.MapToEstimateNoteResponseDto(note);

		return Result<EstimateNoteResponseDto>.Success(responseDto);
	}

	public async Task<Result> DeleteEstimateNote(int estimateId, int noteId, CancellationToken ct)
	{
		var estimate = await repository
			.Query<Estimate>()
			.ByEstimateID(estimateId)
			.Include(e => e.RequestedEstimate)
				.ThenInclude(re => re.Project)
					.ThenInclude(p => p.Lead)
						.ThenInclude(l => l.Address)
			.FirstOrDefaultAsync(ct);

		if (estimate == null)
			return Result<EstimateNoteResponseDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToEstimate(estimate, ct);

		if (!accessResult.IsSuccess)
			return Result<EstimateNoteResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);

		var note = await repository
			.Query<EstimateNote>()
			.ByEstimateId(estimateId)
			.ByEstimateNoteId(noteId)
			.ByIsDeleted(false)
			.FirstOrDefaultAsync(ct);

		if (note == null)
			return Result.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		note.IsDeleted = true;
		note.DateDeleted = DateTimeOffset.UtcNow;

		repository.AddOrUpdate(note);
		await repository.SaveAsync(ct);

		return Result.Success();
	}
}
