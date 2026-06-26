using EFRepository;
using FMFlow.Admin.Interface;
using FMFlow.Admin.Interface.DTOs;
using FMFlow.Admin.Service.Mappers;
using FMFlow.Entities;
using FMFlow.Files.Interface;
using FMFlow.Files.Interface.DTOs;
using FMFlow.FlowAPI.Interface;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.Admin.Service;

public class ColorsService(IRepository repository, IFilesService filesService) : IColorsService
{
	public async Task<Result<List<ColorResponseDto>>> GetColors(
		string? keyword,
		int? paintId,
		TintCategory? tintCategory,
		DateTimeOffset? updatedAfter,
		CancellationToken ct)
	{
		var query = repository.Query<Color>()
			.ByIsDeleted(false);

		if (!string.IsNullOrEmpty(keyword))
		{
			if (keyword.Length < 3 || keyword.Length > 100)
				return Result<List<ColorResponseDto>>.Failure("The keyword should be between 3 to 100 characters.");

			query = query
				.Where(c =>
				EF.Functions.ILike(c.Name, $"%{keyword}%") ||
				(c.SWColorId != null && EF.Functions.ILike(c.SWColorId, $"%{keyword}%")));
		}

		if (paintId.HasValue)
		{
			var paintTintCategory = await repository
				.Query<Paint>()
				.Where(p => p.PaintId == paintId.Value)
				.Select(p => p.TintCategory)
				.FirstOrDefaultAsync(ct);

			query = query.Where(c => c.PaintId == paintId.Value || (paintTintCategory != null && paintTintCategory == c.TintCategory));
		}

		if (tintCategory.HasValue)
			query = query.Where(c => c.TintCategory == tintCategory.Value);

		if (updatedAfter.HasValue)
		{
			// Convert query param to UTC since Npgsql only supports comparing DateTimeOffset as UTC
			var utcLastUpdated = updatedAfter.Value.ToUniversalTime();

			query = query.Where(c => c.DateUpdated >= utcLastUpdated);
		}

		var colorDtos = await query
			.AsNoTracking()
			.Select(c => ColorMapper.MapToColorResponseDto(c))
			.ToListAsync(ct);

		return Result<List<ColorResponseDto>>.Success(colorDtos);
	}

	public async Task<Result<FileDownloadResultDto>> DownloadPicture(int colorId, int fileId, CancellationToken ct)
	{
		var color = await repository
			.Query<Color>()
			.ByColorId(colorId)
			.ByPictureFileId(fileId)
			.Include(c => c.PictureFile)
			.FirstOrDefaultAsync(ct);

		if (color == null || color.PictureFile == null)
			return Result<FileDownloadResultDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		ct.ThrowIfCancellationRequested();

		var fileResult = await filesService.GetFile(color.PictureFile.Key, ct);
		fileResult.Value!.FileName = color.PictureFile.Name;

		return fileResult;
	}
}
