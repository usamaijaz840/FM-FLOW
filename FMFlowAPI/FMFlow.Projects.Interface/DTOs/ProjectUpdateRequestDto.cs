using FMFlow.Entities;

namespace FMFlow.Projects.Interface.DTOs;

public record ProjectUpdateRequestDto(
	int? LeadID,
	string? AddressLine1,
	string? AddressLine2,
	string? City,
	string? State,
	string? ZipCode,
	string? Title,
	string? Summary,
	DateTimeOffset? ScheduleDateTime,
	string? SelectedPaintColors,
	int? ApproxSquareFootage,
	bool? IsOpen = true
);
