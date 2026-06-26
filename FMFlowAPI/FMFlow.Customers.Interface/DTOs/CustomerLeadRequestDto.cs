using FMFlow.Entities;

namespace FMFlow.Customers.Interface.DTOs;

public record CustomerLeadRequestDto
{
	// LeadID is not needed for customer leads
	// ProUserID is not applicable for customer leads
	public int? LeadSourceID { get; init; }
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
	public string? Notes { get; init; } // Added Notes field to match LeadRequestDto
	public CustomerType CustomerType { get; init; } // Non-nullable as it's required for customer leads
	public string? OrganizationName { get; init; }
	public string? ReCaptchaToken { get; init; }
}
