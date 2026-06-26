using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FMFlow.Entities.ModelInterfaces;

namespace FMFlow.Entities;

public class Lead : IHasDateChangeTracking, IHasDeleted
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int LeadID { get; set; }

	[ForeignKey(nameof(LeadSource))]
	public int? LeadSourceID { get; set; } // Used for REFERAL SOURCE leads (ProUserID null)

	public virtual LeadSource LeadSource { get; set; } = null!;

	[ForeignKey(nameof(FlowUser))]
	public int? ProUserID { get; set; } // Used for MANUAL leads created by pros (LeadSourceID null)

	[NotMapped]
	public bool IsReferralSource => LeadSourceID.HasValue;

	public virtual FlowUser? ProUser { get; set; }

	[ForeignKey(nameof(Customer))]
	public int? CustomerID { get; set; }

	public virtual FlowUser Customer { get; set; } = null!;

	[Required]
	public string FirstName { get; set; } = null!;

	[Required]
	public string LastName { get; set; } = null!;

	[Required]
	[ForeignKey(nameof(Address))]
	public int AddressID { get; set; }

	public virtual Address Address { get; set; } = null!;

	[Required]
	public string Email { get; set; } = null!;

	[Required]
	public string Mobile { get; set; } = null!;

	public string? PhoneNumber { get; set; }

	public string? Notes { get; set; }

	public string? OrganizationName { get; set; }

	public CustomerType CustomerType { get; set; }


	[Obsolete("This is no longer needed. It will be removed in a future release.")]
	public string? AccountCreationNonce { get; set; }

	[Obsolete("This is no longer needed. It will be removed in a future release.")]
	public DateTimeOffset? NonceExpirationTime { get; set; }

	[Required]
	public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;

	public DateTimeOffset? DateUpdated { get; set; }

	public bool IsDeleted { get; set; }

	public DateTimeOffset? DateDeleted { get; set; }

	[ForeignKey(nameof(Scheduler))]
	public int? SchedulerId { get; set; }

	public virtual FlowUser Scheduler { get; set; } = null!;

	public bool ScheduleComplete { get; set; } = false;

	public bool CanSetScheduleComplete { get; set; } = false;

	public List<Project>? Projects { get; set; }

	public string GetFullName() =>
		$"{FirstName} {LastName}";
}

public enum CustomerType
{
	Residential = 0,
	LocalCommercial = 1
}
