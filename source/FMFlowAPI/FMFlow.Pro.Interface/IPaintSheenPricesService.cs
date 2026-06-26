using FMFlow.Common;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Pro.Interface.Dtos;

namespace FMFlow.Pro.Interface;

public interface IPaintSheenPricesService
{
	Task<Result<PaintSheenPriceResponseDto>> CreatePaintSheenAndPaintSheenPrice(PaintSheenPriceRequestDto request, CancellationToken ct);

	Task<Result> DeletePaintSheenAndPaintSheenPrice(int paintSheenId, CancellationToken ct);

	Task<Result<SearchResult<PaintSheenPriceResponseDto>>> GetPaintSheensAndPaintSheenPrices(PaintAreaType? paintAreaType, int pageIndex, int pageSize, CancellationToken ct);

	Task<Result<PaintSheenPriceResponseDto>> UpdatePaintSheenAndPaintSheenPrice(int paintSheenId, PaintSheenPriceUpdateRequestDto request, CancellationToken ct);

	Task<Result<PaintSheenPriceResponseDto>> GetPaintSheenPriceByPaintIdAndSheenId(int paintId, int sheenId, CancellationToken ct);
}
