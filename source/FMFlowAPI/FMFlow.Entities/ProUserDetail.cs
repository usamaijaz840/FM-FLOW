using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FMFlow.Entities.ModelInterfaces;

namespace FMFlow.Entities;

public class ProUserDetail : IHasDateChangeTracking
{
	public ProUserDetail()
	{
		BusinessAddress = new Address();
		SherwinHomeAddress = new Address();
	}

	[Key]
	[ForeignKey(nameof(FlowUser))]
	public int UserID { get; set; }

	[Required]
	public string BusinessType { get; set; } = string.Empty;

	[Required]
	public string TaxID { get; set; } = string.Empty;

	[Required]
	public string NumberOfEmployees { get; set; } = string.Empty;

	public string?[]? SizeOfJob { get; set; }

	public string?[]? Services { get; set; }

	[ForeignKey(nameof(Address))]
	public int? SherwinHomeAddressID { get; set; }

	public virtual Address SherwinHomeAddress { get; set; }

	public string? BusinessName { get; set; } = string.Empty;

	[ForeignKey(nameof(Address))]
	public int? BusinessAddressID { get; set; }

	public virtual Address BusinessAddress { get; set; }

	public bool IsApproved { get; set; } = false;

	public bool? RequestedReferrals { get; set; }

	public string? Description { get; set; } = string.Empty;

	public string? GoogleReview { get; set; } = string.Empty;

	public string? YelpReview { get; set; } = string.Empty;

	[StringLength(50)]
	public string? OnboardingFormStop { get; set; }

	[ForeignKey(nameof(Billing))]
	public int? BillingID { get; set; }

	public DateTimeOffset? InsuranceExpDate { get; set; }

	public DateTimeOffset DateCreated { get; set; } = DateTime.UtcNow;

	public DateTimeOffset? DateUpdated { get; set; }

	public virtual FlowUser? FlowUser { get; set; }

	[ForeignKey(nameof(FMTimeZone))]
	public int FMTimeZoneID { get; set; }

	public DateTimeOffset? DateAssignedToRsLead { get; set; }

	public virtual FMTimeZone? FMTimeZone { get; set; }

	public virtual Billing? Billing { get; set; }

	public virtual List<ProUserToProZipcode> ProUserToProZipcodes { get; set; } = [];
}
