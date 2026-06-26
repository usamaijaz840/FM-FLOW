namespace FMFlow.Leads.Interface.DTOs;

public record LeadNoteResponseDto(
	int LeadNoteId,
	int LeadId,
	int? ProId,
	string Note,
	DateTimeOffset DateCreated,
	DateTimeOffset? DateUpdated);
