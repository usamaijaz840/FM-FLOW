using FMFlow.Entities;

namespace FMFlow.Leads.Interface.DTOs;

public record LeadResponseDto(
	int LeadID,
	int? LeadSourceID,
	string? LeadSourceName,
	bool IsReferralSource,
	string FirstName,
	string LastName,
	string AddressLine1,
	string? AddressLine2,
	string City,
	string State,
	string ZipCode,
	string Email,
	string Mobile,
	string? PhoneNumber,
	string? Notes,
	int? ProUserID,
	CustomerType CustomerType,
	string? OrganizationName,
	int? SchedulerId,
	string? SchedulerName,
	bool? ScheduleComplete,
	bool? CanSetScheduleComplete)
{
	public List<int> DuplicateLeadIDs { get; set; } = [];

	public int DuplicateGroupKey { get; set; }
}

