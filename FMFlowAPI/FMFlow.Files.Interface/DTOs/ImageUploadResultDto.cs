
namespace FMFlow.Files.Interface.DTOs;

public record ImageUploadResultDto : FileUploadResultDto
{
	public int? ThumbnailFileId { get; set; }

	public string ThumbnailFileName { get; set; } = string.Empty;

	public string ThumbnailFilePath { get; set; } = string.Empty;

}
