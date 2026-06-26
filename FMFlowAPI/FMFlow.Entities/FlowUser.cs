using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FMFlow.Entities.ModelInterfaces;

namespace FMFlow.Entities;

public class FlowUser() : IHasDateChangeTracking, IHasDeleted
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int? UserID { get; set; }

	[Required]
	public Guid IdentityGuid { get; set; }

	[Required]
	public string FirstName { get; set; } = string.Empty;

	[Required]
	public string LastName { get; set; } = string.Empty;

	[Required]
	public string Email { get; set; } = string.Empty;

	[Required]
	public string PhoneNumber { get; set; } = string.Empty;

	public ProUserDetail? ProUser { get; set; }

	public EmployeeUser? EmployeeUser { get; set; }

	public long? MxCustomerId { get; set; }

	[Required]
	public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;

	public DateTimeOffset? DateUpdated { get; set; }

	public bool IsDeleted { get; set; }

	public DateTimeOffset? DateDeleted { get; set; }

	public virtual List<ProWeekDayAvailability> ProWeekDayAvailabilities { get; set; } = [];

	public string GetFullName() => $"{FirstName} {LastName}";
}
