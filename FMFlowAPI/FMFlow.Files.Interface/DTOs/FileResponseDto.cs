using static FMFlow.Entities.FileItemToEstimate;

namespace FMFlow.Files.Interface.DTOs;

public record FileResponseDto
{
	public int FileID { get; init; }

	public string FileName { get; init; } = string.Empty;

	public string FilePath { get; init; } = string.Empty;

	public string? ContentType { get; init; }

	public EstimateFileType EstimateFileType { get; init; }

	public int? ThumbnailFileID { get; init; }

	public string? ThumbnailFileName { get; init; }

	public string? ThumbnailFilePath { get; init; }

}
