using FMFlow.Entities;
using FMFlow.Estimates.Interface.DTOs;

namespace FMFlow.Projects.Interface.DTOs;

public record ProjectRequestDto(
	int LeadID,
	string AddressLine1,
	string? AddressLine2,
	string City,
	string State,
	string ZipCode,
	string Title,
	string? Summary,
	ProjectRequestedEstimateRequestDto[] RequestedEstimates,
	DateTimeOffset? ScheduleDateTime,
	string? SelectedPaintColors,
	int? ApproxSquareFootage,
	bool IsOpen = true
);
