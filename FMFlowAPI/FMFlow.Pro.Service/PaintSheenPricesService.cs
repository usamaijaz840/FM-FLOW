using EFRepository;
using FluentValidation;
using FMFlow.Common;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Identity.Interface;
using FMFlow.Pro.Interface;
using FMFlow.Pro.Interface.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.Pro.Service;

public class PaintSheenPricesService(
	IRepository repository,
	ICurrentUserService currentUserService,
	IValidator<PaintSheenPriceRequestDto> validator) : IPaintSheenPricesService
{
	public async Task<Result<SearchResult<PaintSheenPriceResponseDto>>> GetPaintSheensAndPaintSheenPrices(PaintAreaType? paintAreaType, int pageIndex, int pageSize, CancellationToken ct)
	{
		var query = repository.Query<PaintSheenPrice>()
			.Include(pp => pp.PaintSheen)
			.ThenInclude(ps => ps.Paint)
			.Include(pp => pp.PaintSheen)
			.ThenInclude(ps => ps.Sheen)
			.ByIsDeleted(false)
			.Where(ps => !ps.IsDeleted);

		if (paintAreaType.HasValue)
		{
			query = query.Where(pp =>
			pp.PaintSheen.Paint.PaintAreaType == paintAreaType.Value ||
			pp.PaintSheen.Paint.PaintAreaType == PaintAreaType.Undefined);
		}

		if (currentUserService.IsPro())
		{
			query = query.Where(pp => pp.ProUserId == currentUserService.GetUserID());
		}

		var totalResults = await query.CountAsync(ct);

		var paintDetails = await query
			.OrderByDescending(pd => pd.DateCreated)
			.Skip(pageIndex * pageSize)
			.Take(pageSize)
			.ToListAsync(ct);

		var dtos = paintDetails
			.Select(pd =>
			{
				return new PaintSheenPriceResponseDto(
					pd.PaintSheenId,
					pd.PaintSheen?.PaintId ?? 0,
					pd.PaintSheen?.Paint.Name ?? "Unknown Paint",
					pd.PaintSheen?.Paint.TintCategory,
					pd.PaintSheen?.SheenId ?? 0,
					pd.PaintSheen?.Sheen?.Name ?? "Unknown Sheen",
					pd.ProUserId,
					currentUserService.IsCustomer() ? null : pd.PricePerGallon,
					pd.PaintSheen?.Paint.GetPictureFileUrl(),
					pd.PaintSheen?.Paint.GetThumbnailPictureFileUrl(),
					pd.PaintSheen?.Paint.SurfacePreparation,
					pd.PaintSheen?.Paint.Warranty,
					pd.PaintSheen?.Paint.Description,
					pd.PaintSheen?.Paint.MarketingCopy,
					pd.PaintSheen?.Paint.ProUserId == null ? true : false
					);
			})
			.ToList();

		var searchResult = new SearchResult<PaintSheenPriceResponseDto>(dtos, totalResults);

		return Result<SearchResult<PaintSheenPriceResponseDto>>.Success(searchResult);
	}

	public async Task<Result<PaintSheenPriceResponseDto>> CreatePaintSheenAndPaintSheenPrice(PaintSheenPriceRequestDto request, CancellationToken ct)
	{
		var requestValidation = await DtoValidator.Validate(request, validator, ct);

		if (!requestValidation.IsSuccess)
			return Result<PaintSheenPriceResponseDto>.Failure(requestValidation.Error!);

		var existingPaint = await repository.Query<Paint>()
			.ByPaintId(request.PaintId)
			.FirstOrDefaultAsync(ct);

		var existingSheen = await repository.Query<Sheen>()
			.BySheenID(request.SheenId)
			.FirstOrDefaultAsync(ct);

		if (existingPaint == null || existingSheen == null)
			return Result<PaintSheenPriceResponseDto>.Failure(ErrorMessages.ResourceNotFound);

		var currentUserId = currentUserService.GetUserID();

		if (existingPaint.ProUserId != null && existingPaint.ProUserId != currentUserId)
			return Result<PaintSheenPriceResponseDto>.Failure(ErrorMessages.ResourceAccessDenied);

		var existingPaintSheenForPro = await repository.Query<PaintSheen>()
				.Where(ps => !ps.IsDeleted &&
							 ps.Paint.ProUserId!.Value == currentUserId &&
							 ps.PaintId == request.PaintId &&
							 ps.SheenId == request.SheenId)
				.AnyAsync(ct);

		if (existingPaintSheenForPro)
			return Result<PaintSheenPriceResponseDto>.Failure("Paint and sheen combination previously created for current Pro User. Update or delete existing one.");

		PaintSheen? paintSheen;

		if (existingPaint.ProUserId != null) // Create new PaintSheen for Pro-specific paints, not for SuperAdmin/Sherwin Williams paints
		{
			paintSheen = new PaintSheen
			{
				PaintId = request.PaintId,
				SheenId = request.SheenId
			};

			repository.AddNew(paintSheen);
		}
		else
		{
			paintSheen = await repository.Query<PaintSheen>()
				.ByPaintId(request.PaintId)
				.BySheenId(request.SheenId)
				.Where(ps => !ps.IsDeleted && ps.Paint.ProUserId == null) // SuperAdmin paints
				.Include(ps => ps.Paint)
				.FirstOrDefaultAsync(ct);

			if (paintSheen == null) // This would mean the admin should've created the paint/sheen combination but didn't
				return Result<PaintSheenPriceResponseDto>.Failure(ErrorMessages.ResourceNotFound);
		}

		if (request.PricePerGallon.HasValue)
		{
			var existingPaintPrice = await repository.Query<PaintSheenPrice>()
				.ByPaintSheenId(paintSheen.PaintSheenId)
				.ByProUserId(request.ProUserId)
				.ByIsDeleted(false)
				.AnyAsync(ct);

			if (existingPaintPrice)
				return Result<PaintSheenPriceResponseDto>.Failure("Paint and sheen price already set for current Pro User. Update or delete existing one.");

			repository.AddNew(new PaintSheenPrice
			{
				PaintSheen = paintSheen,
				PricePerGallon = request.PricePerGallon.Value,
				ProUserId = currentUserId
			});
		}

		await repository.SaveAsync(ct);

		var response = new PaintSheenPriceResponseDto(
			paintSheen.PaintSheenId,
			paintSheen.PaintId,
			existingPaint.Name,
			paintSheen.Paint?.TintCategory,
			paintSheen.SheenId,
			existingSheen.Name,
			currentUserId,
			request.PricePerGallon,
			paintSheen.Paint?.GetPictureFileUrl(),
			paintSheen.Paint?.GetThumbnailPictureFileUrl(),
			paintSheen.Paint?.SurfacePreparation,
			paintSheen.Paint?.Warranty,
			paintSheen.Paint?.MarketingCopy,
			paintSheen.Paint?.Description,
			paintSheen.Paint?.ProUserId == null ? true : false
			);

		return Result<PaintSheenPriceResponseDto>.Success(response);
	}

	public async Task<Result<PaintSheenPriceResponseDto>> UpdatePaintSheenAndPaintSheenPrice(int paintSheenId, PaintSheenPriceUpdateRequestDto request, CancellationToken ct)
	{
		if (!currentUserService.IsPro())
			return Result<PaintSheenPriceResponseDto>.Failure(ErrorMessages.ResourceAccessDenied);

		if (request.PricePerGallon <= 0)
			return Result<PaintSheenPriceResponseDto>.Failure("Price per gallon must be greater than zero.");

		var paintSheen = await repository.Query<PaintSheen>()
			.Include(ps => ps.Paint)
			.Include(ps => ps.Sheen)
			.ByPaintSheenId(paintSheenId)
			.FirstOrDefaultAsync(ct);

		if (paintSheen == null)
			return Result<PaintSheenPriceResponseDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var paintPrice = await repository.Query<PaintSheenPrice>()
			.ByPaintSheenId(paintSheenId)
			.FirstOrDefaultAsync(ct);

		if (paintPrice == null)
			return Result<PaintSheenPriceResponseDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		if (paintPrice.ProUserId != currentUserService.GetUserID())
			return Result<PaintSheenPriceResponseDto>.Failure(ErrorMessages.ResourceAccessDenied);

		if (paintSheen.IsDeleted || paintPrice.IsDeleted)
			return Result<PaintSheenPriceResponseDto>.Failure(ErrorMessages.ResourceDeleted);

		paintPrice.PricePerGallon = request.PricePerGallon;

		repository.AddOrUpdate(paintPrice);
		await repository.SaveAsync(ct);

		var response = new PaintSheenPriceResponseDto(
			paintSheen.PaintSheenId,
			paintSheen.PaintId,
			paintSheen.Paint?.Name ?? "Unknown Paint",
			paintSheen.Paint?.TintCategory,
			paintSheen.SheenId,
			paintSheen.Sheen?.Name ?? "Unknown Sheen",
			paintPrice.ProUserId,
			paintPrice.PricePerGallon,
			paintSheen.Paint?.GetPictureFileUrl(),
			paintSheen.Paint?.GetThumbnailPictureFileUrl(),
			paintSheen.Paint?.SurfacePreparation,
			paintSheen.Paint?.Warranty,
			paintSheen.Paint?.MarketingCopy,
			paintSheen.Paint?.Description,
			paintSheen.Paint?.ProUserId == null ? true : false
			);

		return Result<PaintSheenPriceResponseDto>.Success(response);
	}

	public async Task<Result> DeletePaintSheenAndPaintSheenPrice(int paintSheenId, CancellationToken ct)
	{
		var paintSheen = await repository.Query<PaintSheen>()
			.ByPaintSheenId(paintSheenId)
			.Include(ps => ps.Paint)
			.FirstOrDefaultAsync(ct);

		if (paintSheen == null)
			return Result<PaintSheenPriceResponseDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		if (paintSheen.Paint.ProUserId != null &&
			paintSheen.Paint.ProUserId != currentUserService.GetUserID())
			return Result<PaintSheenPriceResponseDto>.Failure(ErrorMessages.ResourceAccessDenied);

		var paintSheenPrice = await repository.Query<PaintSheenPrice>()
			.ByPaintSheenId(paintSheenId)
			.ByProUserId(currentUserService.GetUserID())
			.FirstOrDefaultAsync(ct);

		if (paintSheenPrice == null)
			return Result<PaintSheenPriceResponseDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		paintSheenPrice.IsDeleted = true;
		paintSheenPrice.DateDeleted = DateTime.UtcNow;
		repository.AddOrUpdate(paintSheenPrice);

		if (paintSheen.Paint.ProUserId == currentUserService.GetUserID())
		{
			paintSheen.IsDeleted = true;
			paintSheen.DateDeleted = DateTime.UtcNow;
			repository.AddOrUpdate(paintSheen);
		}

		await repository.SaveAsync(ct);

		return Result.Success();
	}

	public async Task<Result<PaintSheenPriceResponseDto>> GetPaintSheenPriceByPaintIdAndSheenId(int paintId, int sheenId, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		var paintSheen = await repository.Query<PaintSheen>()
			.Include(ps => ps.Paint)
			.Include(ps => ps.Sheen)
			.Where(ps => !ps.IsDeleted)
			.Where(ps => ps.PaintId == paintId && ps.SheenId == sheenId)
			.FirstOrDefaultAsync(ct);

		if (paintSheen == null)
			return Result<PaintSheenPriceResponseDto>.Failure("Paint sheen combination not found", ResultErrorType.NotFound);

		var paintSheenPrice = await repository.Query<PaintSheenPrice>()
			.Where(pp => !pp.IsDeleted)
			.Where(pp => pp.PaintSheenId == paintSheen.PaintSheenId)
			.ByProUserId(currentUserService.GetUserID())
			.FirstOrDefaultAsync(ct);

		if (paintSheenPrice == null)
			return Result<PaintSheenPriceResponseDto>.Failure("Paint sheen price not found", ResultErrorType.NotFound);

		var response = new PaintSheenPriceResponseDto(
			paintSheenPrice.PaintSheenId,
			paintSheen.PaintId,
			paintSheen.Paint?.Name ?? "Unknown Paint",
			paintSheen.Paint?.TintCategory,
			paintSheen.SheenId,
			paintSheen.Sheen?.Name ?? "Unknown Sheen",
			paintSheenPrice.ProUserId,
			paintSheenPrice.PricePerGallon,
			paintSheen.Paint?.GetPictureFileUrl(),
			paintSheen.Paint?.GetThumbnailPictureFileUrl(),
			paintSheen.Paint?.SurfacePreparation,
			paintSheen.Paint?.Warranty,
			paintSheen.Paint?.MarketingCopy,
			paintSheen.Paint?.Description,
			paintSheen.Paint?.ProUserId == null ? true : false
			);

		return Result<PaintSheenPriceResponseDto>.Success(response);
	}
}
