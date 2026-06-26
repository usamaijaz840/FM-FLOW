namespace FMFlow.Projects.Interface.DTOs;

public record ProjectNoteResponseDto(
	int ProjectNoteId,
	int ProjectId,
	int? ProId,
	string Note,
	DateTimeOffset DateCreated,
	DateTimeOffset? DateUpdated); 