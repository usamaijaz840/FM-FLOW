namespace FMFlow.Files.Interface.DTOs;

public record FileUploadRequestDto
{
	public string FileName { get; set; }

	public string ContentType { get; set; }

	public byte[] FileBytes { get; set; }
}
