namespace FMFlow.Pro.Interface.Dtos;

public record BasicProResponseDto
{
	public int? UserID { get; init; }
	public string FirstName { get; init; } = string.Empty;
	public string LastName { get; init; } = string.Empty;
	public string Email { get; init; } = string.Empty;
	public string? BusinessName { get; init; }
	public List<string>? ZipCodesPendingAMAssignment { get; init; }
	public bool IsDeleted { get; init; }
	public bool IsApproved { get; init; }
}
