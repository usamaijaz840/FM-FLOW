namespace FMFlow.Employees.Interface.DTOs;

public class SearchEmployeeAndRoleDto
{
	public int UserID { get; set; }
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string EmailAddress { get; set; } = string.Empty;
	public string Role { get; set; } = string.Empty;
	public bool IsDeleted { get; set; }
}
