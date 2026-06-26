
namespace FMFlow.Files.Interface.DTOs;

public record FileUploadResultDto
{
	public int FileId { get; set; }

	public string FileName { get; set; } = string.Empty;

	public string FilePath { get; set; } = string.Empty;

	public string? ContentType { get; init; }

	public string? Message { get; set; }
}
