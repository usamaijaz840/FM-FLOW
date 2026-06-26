
namespace FMFlow.Files.Interface.DTOs;

public record FileDownloadResultDto
{
	public byte[] FileBytes { get; set; }
	public string ContentType { get; set; }
	public string FileName { get; set; }
}
