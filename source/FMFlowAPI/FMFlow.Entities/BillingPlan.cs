namespace FMFlow.Entities;

public class BillingPlan
{
	public int BillingPlanID { get; set; }
	public BillingFrequency BillingFrequency { get; set; }
	public decimal Amount { get; set; }
	public string Description { get; set; } = string.Empty;
	public bool IsActive { get; set; } = true;
	public DateTimeOffset DateCreated { get; set; } = DateTime.UtcNow;
}
