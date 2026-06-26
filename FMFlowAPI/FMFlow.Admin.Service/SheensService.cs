using EFRepository;
using FluentValidation;
using FMFlow.Admin.Interface;
using FMFlow.Admin.Interface.DTOs;
using FMFlow.Admin.Service.Mappers;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Identity.Interface;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.Admin.Service;

public class SheensService(
	IRepository repository,
	IValidator<SheenRequestDto> validatorSheenRequestDto,
	ICurrentUserService currentUserService) : ISheensService
{
	public async Task<Result<IEnumerable<SheenResponseDto>>> GetSheens(bool shouldIncludeDeletedSheens, CancellationToken ct)
	{
		var query = repository
			.Query<Sheen>()
			.ByIsDeleted(shouldIncludeDeletedSheens == true ? null : false)
			.AsNoTracking();

		var mapper = new SheenMapper();

		var sheens = query
			.OrderBy(l => l.Name)
			.Select(s => mapper.MapToResponse(s));

		return Result<IEnumerable<SheenResponseDto>>.Success(sheens);
	}

	public async Task<Result<SheenResponseDto>> CreateSheen(SheenRequestDto request, CancellationToken ct)
	{
		var requestValidation = await DtoValidator.Validate(request, validatorSheenRequestDto, ct);

		if (!requestValidation.IsSuccess)
		{
			return Result<SheenResponseDto>.Failure(requestValidation.Error!);
		}

		ct.ThrowIfCancellationRequested();

		var nameAlreadyInUse = await repository.Query<Sheen>()
			.Where(s =>
			s.Name.ToLower() == request.Name.ToLower() &&
			s.IsDeleted == false)
			.AnyAsync(ct);

		if (nameAlreadyInUse)
		{
			return Result<SheenResponseDto>.Failure($"The Sheen with the name of '{request.Name}' already exists.");
		}

		var mapper = new SheenMapper();
		var sheen = mapper.MapToEntity(request);
		sheen.DateCreated = DateTime.UtcNow;
		sheen.UserId = currentUserService.GetUserID();

		repository.AddNew(sheen);
		await repository.SaveAsync(ct);

		var response = mapper.MapToResponse(sheen);

		return Result<SheenResponseDto>.Success(response);
	}

	public async Task<Result<SheenResponseDto>> UpdateSheen(int sheenId, SheenRequestDto request, CancellationToken ct)
	{
		var requestValidation = await DtoValidator.Validate(request, validatorSheenRequestDto, ct);

		if (!requestValidation.IsSuccess)
		{
			return Result<SheenResponseDto>.Failure(requestValidation.Error!);
		}

		var foundSheen = await repository.Query<Sheen>()
			.BySheenID(sheenId)
			.FirstOrDefaultAsync(ct);

		if (foundSheen == null)
		{
			return Result<SheenResponseDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);
		}

		var nameAlreadyInUse = await repository.Query<Sheen>()
			.Where(x => x.SheenID != sheenId)
			.ByName(request.Name)
			.AnyAsync(ct);

		if (nameAlreadyInUse)
		{
			return Result<SheenResponseDto>.Failure("Name already exists.");
		}

		foundSheen.Name = request.Name;
		foundSheen.Description = request.Description;

		if (request.IsDeleted)
		{
			foundSheen.DateDeleted = DateTime.UtcNow;
		}
		else
		{
			foundSheen.DateDeleted = null;
		}

		foundSheen.IsDeleted = request.IsDeleted;
		foundSheen.DateUpdated = DateTime.UtcNow;
		foundSheen.UserId = currentUserService.GetUserID();

		repository.AddOrUpdate(foundSheen);
		await repository.SaveAsync(ct);

		var mapper = new SheenMapper();
		var response = mapper.MapToResponse(foundSheen);

		return Result<SheenResponseDto>.Success(response);
	}

	public async Task<Result> UpdateSheenDeletedStatus(int sheenId, bool isDeleted, CancellationToken ct)
	{
		var sheen = await repository.Query<Sheen>()
			.BySheenID(sheenId)
			.FirstOrDefaultAsync(ct);

		ct.ThrowIfCancellationRequested();

		if (sheen == null)
		{
			return Result.Failure(ErrorMessages.ResourceNotFound);
		}

		if (isDeleted)
		{
			sheen.DateDeleted = DateTime.UtcNow;
		}
		else
		{
			sheen.DateDeleted = null;
		}

		sheen.IsDeleted = isDeleted;

		ct.ThrowIfCancellationRequested();
		repository.AddOrUpdate(sheen);
		await repository.SaveAsync(ct);

		return Result.Success();
	}

}
