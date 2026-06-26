using EFRepository;
using FluentValidation;
using FMFlow.Admin.Interface;
using FMFlow.Admin.Interface.DTOs;
using FMFlow.Admin.Service.Mappers;
using FMFlow.Common;
using FMFlow.Entities;
using FMFlow.Files.Interface;
using FMFlow.Files.Interface.DTOs;
using FMFlow.FlowAPI.Interface;
using FMFlow.Identity.Interface;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.Admin.Service;

public class PaintsService(
	IRepository repository,
	IValidator<PaintRequestDto> validatorPaintRequestDto,
	ICurrentUserService currentUserService,
	IFilesService filesService) : IPaintsService
{
	public async Task<Result<SearchResult<PaintResponseDto>>> GetPaints(
		bool includeDeleted,
		string? keywordSearch,
		int pageIndex,
		int pageSize,
		CancellationToken ct)
	{
		var query = repository
			.Query<Paint>()
			.Include(p => p.PaintSheens!)
				.ThenInclude(ps => ps.Sheen)
			.ByIsDeleted(includeDeleted == true ? null : false)
			.AsNoTracking();

		var currentUserId = currentUserService.GetUserID();

		if (currentUserService.IsPro())
		{
			query = query.Where(p => p.ProUserId == null || p.ProUserId == currentUserId);
		}

		if (!string.IsNullOrWhiteSpace(keywordSearch))
		{
			query = query.Where(p =>
				EF.Functions.ILike(p.Name, $"%{keywordSearch}%") ||
				EF.Functions.ILike(p.Description, $"%{keywordSearch}%"));
		}

		var totalResults = await query.CountAsync(ct);

		var paints = await query
			.Skip(pageIndex * pageSize)
			.Take(pageSize)
			.OrderBy(l => l.Name)
			.ToListAsync(ct);

		var allSheens = await repository
			.Query<Sheen>()
			.ByIsDeleted(false)
			.AsNoTracking()
			.ToListAsync(ct);

		var allSheensDtos = SheenMapper.MapSheensToResponse(allSheens);

		var paintsDtos = paints
			.Select(paint =>
			{
				var dto = PaintMapper.MapToResponse(paint);

				if (paint.PaintSheens != null && paint.ProUserId == null)
				{
					var sheens = paint.PaintSheens
						.Select(ps => new SheenInPaintResponseDto(ps.SheenId, ps.Sheen.Name))
						.Distinct()
						.OrderBy(ps => ps.SheenName)
						.ToList();

					dto = dto with { Sheens = sheens };
				}
				else
				{
					dto = dto with { Sheens = allSheensDtos };
				}

				return dto;
			})
			.ToList();

		var searchResult = new SearchResult<PaintResponseDto>(paintsDtos, totalResults);

		return Result<SearchResult<PaintResponseDto>>.Success(searchResult);
	}

	public async Task<Result<SearchResult<PaintResponseDto>>> GetPaintsByPaintAreaType(PaintAreaType? paintAreaType, CancellationToken ct)
	{
		var query = repository
			.Query<Paint>()
			.ByIsDeleted(false)
			.AsNoTracking();

		// Filter paints for Pro users - they should only see admin paints (ProUserId is null) and their own custom paints
		if (currentUserService.IsPro())
		{
			query = query.Where(p => p.ProUserId == null || p.ProUserId == currentUserService.GetUserID());
		}

		if (paintAreaType != null)
		{
			query = query.Where(p => p.PaintAreaType == paintAreaType || p.PaintAreaType == PaintAreaType.InteriorAndExterior);
		}

		var totalResults = await query.CountAsync(ct);

		var paints = query
			.OrderBy(l => l.Name)
			.Select(p => PaintMapper.MapToResponse(p));

		var searchResult = new SearchResult<PaintResponseDto>(paints, totalResults);
		return Result<SearchResult<PaintResponseDto>>.Success(searchResult);
	}

	public async Task<Result<PaintResponseDto>> CreatePaint(PaintRequestDto request, CancellationToken ct)
	{
		var requestValidation = await DtoValidator.Validate(request, validatorPaintRequestDto, ct);

		if (!requestValidation.IsSuccess)
		{
			return Result<PaintResponseDto>.Failure(requestValidation.Error!);
		}

		if (currentUserService.IsPro() && !request.ProUserId.HasValue)
		{
			request = request with { ProUserId = currentUserService.GetUserID() };
		}

		ct.ThrowIfCancellationRequested();

		int? proId = null;

		if (currentUserService.IsPro())
			proId = currentUserService.GetUserID();

		var nameAlreadyInUse = await repository.Query<Paint>()
			.Where(p =>
			p.Name.ToLower() == request.Name.ToLower() &&
			p.IsDeleted == false &&
			p.ProUserId == proId)
			.AnyAsync(ct);

		if (nameAlreadyInUse)
		{
			return Result<PaintResponseDto>.Failure($"The Paint with the name of '{request.Name}' already exists.");
		}

		var mapper = new PaintMapper();
		var paint = mapper.MapToEntity(request);
		paint.DateCreated = DateTime.UtcNow;

		repository.AddNew(paint);
		await repository.SaveAsync(ct);

		var response = PaintMapper.MapToResponse(paint);

		if (paint.ProUserId == null) // (Sherwin Williams paint)
		{
			var paintSheens = await repository.Query<PaintSheen>()
				.ByPaintId(paint.PaintId)
				.ToListAsync(ct);

			var sheens = paintSheens
				.Select(ps => new SheenInPaintResponseDto(ps.SheenId, ps.Sheen.Name))
				.Distinct()
				.OrderBy(ps => ps.SheenName)
				.ToList();

			response = response with { Sheens = sheens };
		}
		else
		{
			var allSheens = await repository
				.Query<Sheen>()
				.ByIsDeleted(false)
				.AsNoTracking()
				.ToListAsync(ct);

			var allSheensDtos = SheenMapper.MapSheensToResponse(allSheens);

			response = response with { Sheens = allSheensDtos };
		}

		return Result<PaintResponseDto>.Success(response);
	}

	public async Task<Result<PaintResponseDto>> UpdatePaint(int paintId, PaintUpdateRequestDto request, CancellationToken ct)
	{
		var foundPaint = await repository.Query<Paint>()
			.ByPaintId(paintId)
			.FirstOrDefaultAsync(ct);

		if (foundPaint == null)
		{
			return Result<PaintResponseDto>.Failure("Paint does not exist.");
		}

		if (string.IsNullOrWhiteSpace(request.Name))
		{
			return Result<PaintResponseDto>.Failure("Paint Name is required.");
		}

		foundPaint.Name = request.Name;
		foundPaint.Description = request.Description;
		foundPaint.DateUpdated = DateTime.UtcNow;
		foundPaint.PaintAreaType = request.PaintAreaType;

		repository.AddOrUpdate(foundPaint);
		await repository.SaveAsync(ct);

		var response = PaintMapper.MapToResponse(foundPaint);
		return Result<PaintResponseDto>.Success(response);
	}

	public async Task<Result> UpdatePaintDeletedStatus(int paintId, bool isDeleted, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		var foundPaint = await repository.Query<Paint>()
			.ByPaintId(paintId)
			.FirstOrDefaultAsync(ct);

		if (foundPaint == null)
		{
			return Result<PaintResponseDto>.Failure($"Paint does not exist.", ResultErrorType.NotFound);
		}

		if (isDeleted)
		{
			foundPaint.DateDeleted = DateTime.UtcNow;
		}
		else
		{
			foundPaint.DateDeleted = null;
		}

		foundPaint.IsDeleted = isDeleted;
		foundPaint.DateUpdated = DateTime.UtcNow;

		repository.AddOrUpdate(foundPaint);
		await repository.SaveAsync(ct);

		return Result.Success();
	}

	public async Task<Result<FileDownloadResultDto>> DownloadPicture(int paintId, int fileId, CancellationToken ct)
	{
		var paint = await repository
			.Query<Paint>()
			.ByPaintId(paintId)
			.ByPictureFileId(fileId)
			.Include(c => c.PictureFile)
			.FirstOrDefaultAsync(ct);

		if (paint == null || paint.PictureFile == null)
		{
			return Result<FileDownloadResultDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);
		}

		ct.ThrowIfCancellationRequested();

		var fileResult = await filesService.GetFile(paint.PictureFile.Key, ct);
		fileResult.Value!.FileName = paint.PictureFile.Name;

		return fileResult;
	}

	public async Task<Result<FileDownloadResultDto>> DownloadThumbnailPicture(int paintId, int fileId, CancellationToken ct)
	{
		var paint = await repository
			.Query<Paint>()
			.ByPaintId(paintId)
			.ByThumbnailFileId(fileId)
			.Include(c => c.ThumbnailFile)
			.FirstOrDefaultAsync(ct);

		if (paint == null || paint.ThumbnailFile == null)
			return Result<FileDownloadResultDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		ct.ThrowIfCancellationRequested();

		var fileResult = await filesService.GetFile(paint.ThumbnailFile.Key, ct);
		fileResult.Value!.FileName = paint.ThumbnailFile.Name;

		return fileResult;
	}

	public async Task<Result<List<PaintResponseDto>>> GetPaintTypes(
		PaintAreaType? paintAreaType,
		bool uniquePaintId,
		bool haveValidSheenPrice,
		CancellationToken ct)
	{
		if (haveValidSheenPrice)
		{
			// Query from PaintSheenPrice table when we need paints with valid prices
			IQueryable<PaintSheenPrice> paintSheenPriceQuery = repository.Query<PaintSheenPrice>()
				.Include(pp => pp.PaintSheen)
				.ThenInclude(ps => ps.Paint)
				.Include(pp => pp.PaintSheen)
				.ThenInclude(ps => ps.Sheen)
				.ByIsDeleted(false);

			// Filter PaintSheenPrices by ProUserId when user is a pro
			if (currentUserService.IsPro())
			{
				paintSheenPriceQuery = paintSheenPriceQuery.ByProUserId(currentUserService.GetUserID());
			}

			if (paintAreaType.HasValue)
			{
				var requestedType = paintAreaType.Value;
				if (requestedType == PaintAreaType.InteriorAndExterior)
				{
					// Return all paint types when requesting InteriorAndExterior
					paintSheenPriceQuery = paintSheenPriceQuery.Where(pp =>
						pp.PaintSheen.Paint.PaintAreaType == PaintAreaType.Interior ||
						pp.PaintSheen.Paint.PaintAreaType == PaintAreaType.Exterior ||
						pp.PaintSheen.Paint.PaintAreaType == PaintAreaType.InteriorAndExterior ||
						pp.PaintSheen.Paint.PaintAreaType == PaintAreaType.Undefined);
				}
				else
				{
					paintSheenPriceQuery = paintSheenPriceQuery.Where(pp =>
						pp.PaintSheen.Paint.PaintAreaType == requestedType ||
						pp.PaintSheen.Paint.PaintAreaType == PaintAreaType.InteriorAndExterior ||
						pp.PaintSheen.Paint.PaintAreaType == PaintAreaType.Undefined);
				}
			}

			paintSheenPriceQuery = paintSheenPriceQuery.Where(pp => pp.PricePerGallon > 0);

			var paintSheenPrices = await paintSheenPriceQuery.ToListAsync(ct);

			var paintsQuery = paintSheenPrices
				.Select(pp => pp.PaintSheen.Paint)
				.Where(p => p != null);
			
			var paints = uniquePaintId
				? paintsQuery.DistinctBy(p => p.PaintId).ToList()
				: paintsQuery.ToList();

			// Filter by custom paints (ProUserId is current user) and generic paints (ProUserId is null)
			if (currentUserService.IsPro())
			{
				paints = paints.Where(p => p.ProUserId == null || p.ProUserId == currentUserService.GetUserID()).ToList();
			}

			var orderedPaints = paints
				.OrderBy(p => p.Name)
				.ToList();

			var paintsDtos = orderedPaints
				.Select(paint =>
				{
					var dto = PaintMapper.MapToResponse(paint);

					// When haveValidSheenPrice = true, return only sheens with valid prices for ALL paints
					var paintSheensWithValidPrices = paintSheenPrices
						.Where(pp => pp.PaintSheen.Paint.PaintId == paint.PaintId)
						.Select(pp => new SheenInPaintResponseDto(pp.PaintSheen.SheenId, pp.PaintSheen.Sheen.Name))
						.Distinct()
						.OrderBy(ps => ps.SheenName)
						.ToList();

					dto = dto with { Sheens = paintSheensWithValidPrices };

					return dto;
				})
				.ToList();

			return Result<List<PaintResponseDto>>.Success(paintsDtos);
		}
		else
		{
			// Query from Paints table directly when we don't need valid prices
			var paintQuery = repository.Query<Paint>()
				.Include(p => p.PaintSheens)
				.ThenInclude(ps => ps.Sheen)
				.ByIsDeleted(false);

			// Filter by custom paints (ProUserId is current user) and generic paints (ProUserId is null)
			if (currentUserService.IsPro())
			{
				paintQuery = paintQuery.Where(p => p.ProUserId == null || p.ProUserId == currentUserService.GetUserID());
			}

			if (paintAreaType.HasValue)
			{
				var requestedType = paintAreaType.Value;
				if (requestedType == PaintAreaType.InteriorAndExterior)
				{
					// Return all paint types when requesting InteriorAndExterior
					paintQuery = paintQuery.Where(p =>
						p.PaintAreaType == PaintAreaType.Interior ||
						p.PaintAreaType == PaintAreaType.Exterior ||
						p.PaintAreaType == PaintAreaType.InteriorAndExterior ||
						p.PaintAreaType == PaintAreaType.Undefined);
				}
				else
				{
					paintQuery = paintQuery.Where(p =>
						p.PaintAreaType == requestedType ||
						p.PaintAreaType == PaintAreaType.InteriorAndExterior ||
						p.PaintAreaType == PaintAreaType.Undefined);
				}
			}

			var paints = await paintQuery
				.OrderBy(p => p.Name)
				.ToListAsync(ct);

			// Fetch all sheens to use for pro custom paints
			var allSheens = await repository
				.Query<Sheen>()
				.ByIsDeleted(false)
				.AsNoTracking()
				.ToListAsync(ct);

			var allSheensDtos = SheenMapper.MapSheensToResponse(allSheens);

			var paintsDtos = paints
				.Select(paint =>
				{
					var dto = PaintMapper.MapToResponse(paint);

					// For pro custom paints, return all sheens. For admin paints, return only PaintSheens
					if (paint.ProUserId != null)
					{
						dto = dto with { Sheens = allSheensDtos };
					}
					else
					{
						// Include sheens from PaintSheens if they exist
						var sheens = paint.PaintSheens?
							.Where(ps => !ps.IsDeleted)
							.Select(ps => new SheenInPaintResponseDto(ps.SheenId, ps.Sheen.Name))
							.Distinct()
							.OrderBy(ps => ps.SheenName)
							.ToList() ?? new List<SheenInPaintResponseDto>();

						dto = dto with { Sheens = sheens };
					}

					return dto;
				})
				.ToList();

			return Result<List<PaintResponseDto>>.Success(paintsDtos);
		}
	}
}
