namespace FMFlow.Employees.Interface.DTOs;

public record EmployeeResponseDto
{
	public int UserID { get; set; }

	public string FirstName { get; set; } = string.Empty;

	public string LastName { get; set; } = string.Empty;

	public string Email { get; set; } = string.Empty;

	public string PhoneNumber { get; set; } = string.Empty;

	public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;

	public DateTimeOffset? DateUpdated { get; set; }

	public DateTimeOffset? DateDeleted { get; set; }

	public string Role { get; set; } = string.Empty;

	public string? Biography { get; set; }

	public string? Memo { get; set; }

	public string? AddressLine1 { get; set; }

	public string? AddressLine2 { get; set; }

	public string? City { get; set; }

	public string? State { get; set; }

	public string? ZipCode { get; set; }

	public decimal? DailyGoal { get; set; }

	public decimal? BurdenRate { get; set; }

	public string? Skills { get; set; }

	public bool IsDeleted { get; set; }

	public string? TwilioNumber { get; set; }

	public string? TwilioCallerID { get; set; }
}
