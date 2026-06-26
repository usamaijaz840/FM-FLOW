namespace FMFlow.Pro.Interface.Dtos;

public record PaintSheenPriceRequestDto(
	int PaintId,
	int SheenId,
	int? ProUserId,
	decimal? PricePerGallon);
