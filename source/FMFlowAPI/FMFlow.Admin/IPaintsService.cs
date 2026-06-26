using FMFlow.Admin.Interface.DTOs;
using FMFlow.Common;
using FMFlow.Entities;
using FMFlow.Files.Interface.DTOs;
using FMFlow.FlowAPI.Interface;

namespace FMFlow.Admin.Interface;

public interface IPaintsService
{
	Task<Result<SearchResult<PaintResponseDto>>> GetPaints(
		bool includeDeleted,
		string? keywordSearch,
		int pageIndex,
		int pageSize,
		CancellationToken ct);

	Task<Result<SearchResult<PaintResponseDto>>> GetPaintsByPaintAreaType(PaintAreaType? paintAreaType, CancellationToken ct);

	Task<Result<PaintResponseDto>> CreatePaint(PaintRequestDto request, CancellationToken ct);

	Task<Result<PaintResponseDto>> UpdatePaint(int paintId, PaintUpdateRequestDto request, CancellationToken ct);

	Task<Result> UpdatePaintDeletedStatus(int paintId, bool isDeleted, CancellationToken ct);

	Task<Result<FileDownloadResultDto>> DownloadPicture(int paintId, int fileId, CancellationToken ct);

	Task<Result<FileDownloadResultDto>> DownloadThumbnailPicture(int paintId, int fileId, CancellationToken ct);

	Task<Result<List<PaintResponseDto>>> GetPaintTypes(
		PaintAreaType? paintAreaType,
		bool uniquePaintId,
		bool haveValidSheenPrice,
		CancellationToken ct);
}
