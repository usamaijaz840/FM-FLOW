using FMFlow.Admin.Interface.DTOs;
using FMFlow.Entities;
using FMFlow.Files.Interface.DTOs;
using FMFlow.FlowAPI.Interface;

namespace FMFlow.Admin.Interface;
public interface IColorsService
{
	Task<Result<List<ColorResponseDto>>> GetColors(
		string? keyword,
		int? paintId,
		TintCategory? tintCategory,
		DateTimeOffset? updatedAfter,
		CancellationToken ct);

	Task<Result<FileDownloadResultDto>> DownloadPicture(int colorId, int fileId, CancellationToken ct);
}
