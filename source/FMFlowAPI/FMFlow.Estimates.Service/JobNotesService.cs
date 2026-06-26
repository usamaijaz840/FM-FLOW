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

public class JobNotesService(
	IRepository repository,
	IValidator<JobNoteRequestDto> validator,
	IAccessValidator accessValidator)
	: IJobNotesService
{
	public async Task<Result<SearchResult<JobNoteResponseDto>>> GetJobNotes(int jobId, int pageIndex, int pageSize, CancellationToken ct)
	{
		var job = await repository
			.Query<Job>()
			.ByJobId(jobId)
			.Include(j => j.Estimate)
				.ThenInclude(e => e.RequestedEstimate)
					.ThenInclude(re => re.Project)
						.ThenInclude(p => p.Lead)
							.ThenInclude(l => l.Address)
			.FirstOrDefaultAsync(ct);

		if (job == null)
			return Result<SearchResult<JobNoteResponseDto>>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToEstimate(job.Estimate, ct);

		if (!accessResult.IsSuccess)
			return Result<SearchResult<JobNoteResponseDto>>.Failure(accessResult.Error!, accessResult.ErrorType);

		var query = repository
			.Query<JobNote>()
			.ByJobId(jobId)
			.ByIsDeleted(false);

		var totalCount = await query.CountAsync(ct);

		var notes = await query
			.OrderByDescending(n => n.DateCreated)
			.Skip(pageIndex * pageSize)
			.Take(pageSize)
			.ToListAsync(ct);

		var mapper = new JobNoteMapper();

		var noteDtos = notes.Select(mapper.MapToJobNoteResponseDto).ToList();

		var result = new SearchResult<JobNoteResponseDto>(noteDtos, totalCount);

		return Result<SearchResult<JobNoteResponseDto>>.Success(result);
	}

	public async Task<Result<JobNoteResponseDto>> CreateJobNote(int jobId, JobNoteRequestDto createNoteRequest, CancellationToken ct)
	{
		var validationResult = await validator.ValidateAsync(createNoteRequest, ct);

		if (!validationResult.IsValid)
			return Result<JobNoteResponseDto>.Failure(validationResult.ToString(), ResultErrorType.BadRequest);

		var job = await repository
			.Query<Job>()
			.ByJobId(jobId)
			.Include(j => j.Estimate)
				.ThenInclude(e => e.RequestedEstimate)
					.ThenInclude(re => re.Project)
						.ThenInclude(p => p.Lead)
							.ThenInclude(l => l.Address)
			.FirstOrDefaultAsync(ct);

		if (job == null)
			return Result<JobNoteResponseDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToEstimate(job.Estimate, ct);

		if (!accessResult.IsSuccess)
			return Result<JobNoteResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);

		var mapper = new JobNoteMapper();
		var newNote = new JobNote();
		mapper.UpdateJobNote(createNoteRequest, newNote);

		// Set additional properties not covered by the mapper
		newNote.JobId = jobId;
		newNote.DateCreated = DateTimeOffset.UtcNow;

		repository.AddNew(newNote);
		await repository.SaveAsync(ct);

		var responseDto = mapper.MapToJobNoteResponseDto(newNote);

		return Result<JobNoteResponseDto>.Success(responseDto);
	}

	public async Task<Result<JobNoteResponseDto>> UpdateJobNote(int jobId, int noteId, JobNoteRequestDto updateNoteRequest, CancellationToken ct)
	{
		var validationResult = await validator.ValidateAsync(updateNoteRequest, ct);

		if (!validationResult.IsValid)
			return Result<JobNoteResponseDto>.Failure(validationResult.ToString(), ResultErrorType.BadRequest);

		var job = await repository
			.Query<Job>()
			.ByJobId(jobId)
			.Include(j => j.Estimate)
				.ThenInclude(e => e.RequestedEstimate)
					.ThenInclude(re => re.Project)
						.ThenInclude(p => p.Lead)
							.ThenInclude(l => l.Address)
			.Include(j => j.Estimate)
			.FirstOrDefaultAsync(ct);

		if (job == null)
			return Result<JobNoteResponseDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToEstimate(job.Estimate, ct);

		if (!accessResult.IsSuccess)
			return Result<JobNoteResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);

		var note = await repository
			.Query<JobNote>()
			.ByJobNoteId(noteId)
			.ByJobId(jobId)
			.ByIsDeleted(false)
			.FirstOrDefaultAsync(ct);

		if (note == null)
			return Result<JobNoteResponseDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var mapper = new JobNoteMapper();

		mapper.UpdateJobNote(updateNoteRequest, note);
		note.DateUpdated = DateTimeOffset.UtcNow;

		repository.AddOrUpdate(note);
		await repository.SaveAsync(ct);

		var responseDto = mapper.MapToJobNoteResponseDto(note);

		return Result<JobNoteResponseDto>.Success(responseDto);
	}

	public async Task<Result> DeleteJobNote(int jobId, int noteId, CancellationToken ct)
	{
		var job = await repository
			.Query<Job>()
			.ByJobId(jobId)
			.Include(j => j.Estimate)
				.ThenInclude(e => e.RequestedEstimate)
					.ThenInclude(re => re.Project)
						.ThenInclude(p => p.Lead)
							.ThenInclude(l => l.Address)
			.Include(j => j.Estimate)
			.FirstOrDefaultAsync(ct);

		if (job == null)
			return Result<JobNoteResponseDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToEstimate(job.Estimate, ct);

		if (!accessResult.IsSuccess)
			return Result<JobNoteResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);

		var note = await repository
			.Query<JobNote>()
			.ByJobNoteId(noteId)
			.ByJobId(jobId)
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
