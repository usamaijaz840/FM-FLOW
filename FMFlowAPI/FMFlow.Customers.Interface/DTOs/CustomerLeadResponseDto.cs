using FMFlow.Identity.Interface.DTOs;

namespace FMFlow.Customers.Interface.DTOs;

public record CustomerLeadResponseDto
{
	public TokenResponseDto CustomerTempToken { get; init; }
	public int? LeadSourceID { get; init; }
	public int LeadID { get; init; }
	public string FirstName { get; init; }
	public string LastName { get; init; }
	public string Email { get; init; }
	public string Mobile { get; init; }
	public string? PhoneNumber { get; init; }
	public string? Notes { get; init; }
	public string? OrganizationName { get; init; }
	public string CustomerType { get; init; }
	public DateTimeOffset DateCreated { get; init; }

	public int AddressID { get; init; }
	public string AddressLine1 { get; init; }
	public string? AddressLine2 { get; init; }
	public string City { get; init; }
	public string State { get; init; }
	public string ZipCode { get; init; }
}
