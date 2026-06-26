using EFRepository;
using FluentValidation;
using FMFlow.Admin.Interface;
using FMFlow.Admin.Interface.DTOs;
using FMFlow.Admin.Service.Mappers;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.Admin.Service;

public class LeadSourcesService(
	IRepository repository,
	IValidator<LeadSourceRequestDto> validatorLeadSourceRequestDto)
	: ILeadSourcesService
{
	public IEnumerable<LeadSourceResponseDto> GetLeadSources(bool includeDeleted, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		var mapper = new LeadSourceMapper();
		var query = repository.Query<LeadSource>().AsNoTracking();

		if (!includeDeleted)
		{
			query = query.Where(ls => ls.IsDeleted == false);
		}

		ct.ThrowIfCancellationRequested();
		return query.Select(ls => mapper.MapToResponse(ls));
	}

	public async Task<Result<LeadSourceResponseDto>> CreateLeadSource(LeadSourceRequestDto request, CancellationToken ct)
	{
		var requestValidation = await DtoValidator.Validate(request, validatorLeadSourceRequestDto, ct);

		if (!requestValidation.IsSuccess)
		{
			return Result<LeadSourceResponseDto>.Failure(requestValidation.Error!);
		}

		ct.ThrowIfCancellationRequested();

		var doesNameExist = await repository.Query<LeadSource>()
			.Where(ls =>
				ls.Name.ToLower() == request.Name.ToLower() &&
				ls.IsDeleted == false)
			.AnyAsync(ct);

		if (doesNameExist)
		{
			return Result<LeadSourceResponseDto>.Failure($"The LeadSource with the name of '{request.Name}' already exists.");
		}

		ct.ThrowIfCancellationRequested();

		var mapper = new LeadSourceMapper();
		var mappedObject = mapper.MapToEntity(request);

		repository.AddNew(mappedObject);
		await repository.SaveAsync(ct);

		var responseDto = mapper.MapToResponse(mappedObject);
		return Result<LeadSourceResponseDto>.Success(responseDto);
	}

	public async Task<Result<LeadSourceResponseDto>> UpdateLeadSource(int leadSourceId, LeadSourceRequestDto request, CancellationToken ct)
	{
		var requestValidation = await DtoValidator.Validate(request, validatorLeadSourceRequestDto, ct);

		if (!requestValidation.IsSuccess)
		{
			return Result<LeadSourceResponseDto>.Failure(requestValidation.Error!);
		}

		var foundLeadSource = await repository.Query<LeadSource>()
			.ByLeadSourceID(leadSourceId)
			.FirstOrDefaultAsync(ct);

		if (foundLeadSource is null)
		{
			return Result<LeadSourceResponseDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);
		}

		if (!string.IsNullOrWhiteSpace(request.Name))
		{
			var foundExistingName = await repository.Query<LeadSource>()
				.ByName(request.Name)
				.ByIsDeleted(false)
				.Where(x => x.LeadSourceID != leadSourceId)
				.FirstOrDefaultAsync(ct);

			if (foundExistingName is not null)
			{
				return Result<LeadSourceResponseDto>.Failure($"The LeadSource with the name of '{foundExistingName.Name}' already exists.");
			}
		}

		ct.ThrowIfCancellationRequested();

		var mapper = new LeadSourceMapper();
		mapper.MapToEntity(request, foundLeadSource);
		foundLeadSource.DateUpdated = DateTime.UtcNow;

		repository.AddOrUpdate(foundLeadSource);
		await repository.SaveAsync(ct);

		var responseDto = mapper.MapToResponse(foundLeadSource);
		return Result<LeadSourceResponseDto>.Success(responseDto);
	}

	public async Task<Result> UpdateLeadSourceDeletedStatus(int leadSourceId, bool isDeleted, CancellationToken ct)
	{
		var foundLeadSource = await repository.Query<LeadSource>()
			.ByLeadSourceID(leadSourceId)
			.FirstOrDefaultAsync(ct);

		ct.ThrowIfCancellationRequested();

		if (foundLeadSource is null)
		{
			return Result.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);
		}

		if (isDeleted)
		{
			foundLeadSource.DateDeleted = DateTime.UtcNow;
		}
		else
		{
			foundLeadSource.DateDeleted = null;
		}

		foundLeadSource.IsDeleted = isDeleted;

		ct.ThrowIfCancellationRequested();
		repository.AddOrUpdate(foundLeadSource);
		await repository.SaveAsync(ct);

		return Result.Success();
	}
}
