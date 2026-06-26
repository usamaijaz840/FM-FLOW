using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FMFlow.Entities;

public class ZipCode
{
	[Key]
	public string Zipcode { get; set; } = string.Empty;
	[ForeignKey(nameof(State))]
	public string StateAbbreviation { get; set; } = string.Empty;
	public string County { get; set; } = string.Empty;
	public State State { get; set; }
	public List<EmployeeUser> EmployeesAssigned { get; set; } = [];

}
