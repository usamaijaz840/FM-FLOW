using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FMFlow.Entities;

[Table("EmployeeUsers")]
public class EmployeeUser
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.None)]
	[ForeignKey(nameof(FlowUser))]
	public int UserID { get; set; }

	public virtual FlowUser FlowUser { get; set; }

	public string Role { get; set; } = string.Empty;

	public string? Biography { get; set; }

	public string? Memo { get; set; }
	
	[ForeignKey(nameof(Address))]
	public int? AddressID { get; set; }

	public virtual Address? Address { get; set; } = null;

	public decimal? DailyGoal { get; set; }

	public decimal? BurdenRate { get; set; }

	public string? Skills { get; set; }

	public bool IsDeleted { get; set; }

	public string? TwilioNumber { get; set; }

	public string? TwilioCallerID { get; set; }

	public List<ZipCode> AssignedZipCodes { get; set; } = [];
}
