using FMFlow.Entities;

namespace FMFlow.ProUser.Interface.DTOs;

public class ProUserFileUploadRequestDto
{
	public byte[] FileBytes { get; set; }
	public string FileName { get; set; }
	public string ContentType { get; set; }
	
	public ProUserFileType ProFileType { get; set; }
}
