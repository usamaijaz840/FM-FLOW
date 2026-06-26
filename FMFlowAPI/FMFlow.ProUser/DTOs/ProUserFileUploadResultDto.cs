using FMFlow.Entities;
using FMFlow.Files.Interface.DTOs;

namespace FMFlow.ProUser.Interface.DTOs;

public record ProUserFileUploadResultDto : ImageUploadResultDto
{
	public ProUserFileType ProFileType { get; set; }
}
