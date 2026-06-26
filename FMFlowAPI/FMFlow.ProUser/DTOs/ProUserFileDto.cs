using FMFlow.Entities;

namespace FMFlow.ProUser.Interface.DTOs;

public record ProUserFileDto(
	int ProUserFileID,
	string FileName,
	string FilePath,
	string? ContentType,
	string ThumbnailFileName,
	string ThumbnailPath,
	ProUserFileType ProUserFileType,
	int FileID
);
