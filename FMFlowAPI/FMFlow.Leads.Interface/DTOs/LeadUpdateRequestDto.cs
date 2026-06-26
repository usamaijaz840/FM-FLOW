using FMFlow.Entities;

namespace FMFlow.Leads.Interface.DTOs;

public record LeadUpdateRequestDto
{
	public int? LeadSourceId { get; init; }
	public string FirstName { get; init; }
	public string LastName { get; init; }
	public string AddressLine1 { get; init; }
	public string? AddressLine2 { get; init; }
	public string City { get; init; }
	public string State { get; init; }
	public string ZipCode { get; init; }
	public string Email { get; init; }
	public string Mobile { get; init; }
	public string? PhoneNumber { get; init; }
	public string? Notes { get; init; }
	public int? ProUserId { get; init; }
	public int? SchedulerId { get; init; }
	public CustomerType? CustomerType { get; init; } // dto enums must be nullable to allow validation for explicit value
	public string? OrganizationName { get; init; }
	public bool? ScheduleComplete { get; init; } // Only used by Schedulers
}
