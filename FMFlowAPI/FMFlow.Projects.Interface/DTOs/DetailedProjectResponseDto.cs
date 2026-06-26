using FMFlow.Entities;

namespace FMFlow.Projects.Interface.DTOs;

public record DetailedProjectResponseDto(
	int ProjectID,
	int LeadID,
	int? CustomerID,
	bool IsReferralSource,
	string AddressLine1,
	string? AddressLine2,
	string City,
	string State,
	string ZipCode,
	string Title,
	string? Summary,
	bool IsOpen,
	bool IsDeleted,
	DateTimeOffset? DateDeleted,
	bool IsDeleteable,
	string? SelectedPaintColors,
	int? ApproxSquareFootage,
	string? LeadFullName,
	string ShortName,
	string LongName,
	int? LeadReferralSourceId)
{
	public List<EstimateJobResponseDto>? Estimates { get; set; }
	public List<EstimateJobResponseDto>? Jobs { get; set; }
}


