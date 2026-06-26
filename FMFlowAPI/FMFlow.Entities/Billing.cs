using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FMFlow.Entities;

public class Billing
{
	[Key]
	public int BillingID { get; set; }

	public long? MerchantID { get; set; }

	public long? CardID { get; set; }

	public string? VaultedCardToken { get; set; }

	public long? ContractID { get; set; }

	public DateTime StartDate { get; set; }

	public DateTime DateCreated { get; set; } = DateTime.UtcNow;

	[ForeignKey(nameof(BillingPlan))]
	public int? BillingPlanID { get; set; }

	public virtual BillingPlan? BillingPlan { get; set; }
}
