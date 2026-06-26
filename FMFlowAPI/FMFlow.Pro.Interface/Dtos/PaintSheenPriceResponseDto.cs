using FMFlow.Entities;

namespace FMFlow.Pro.Interface.Dtos;

public record PaintSheenPriceResponseDto(
	int PaintSheenId,
	int PaintId,
	string PaintName,
	TintCategory? PaintTintCategory,
	int SheenId,
	string SheenName,
	int? ProUserId,
	decimal? PricePerGallon,
	string? PaintPictureUrl,
	string? PaintThumbnailPictureUrl,
	string? SurfacePreparation,
	string? Warranty,
	string? MarketingCopy,
	string? Description,
	bool IsSherwinWilliamsPaint
	);

