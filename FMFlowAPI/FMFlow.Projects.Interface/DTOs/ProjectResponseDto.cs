using FMFlow.Entities;

namespace FMFlow.Projects.Interface.DTOs;

public record ProjectResponseDto(
	int ProjectID,
	int LeadID,
	int? CustomerID,
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
	string LongName);
