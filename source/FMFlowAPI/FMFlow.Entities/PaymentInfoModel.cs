namespace FMFlow.Entities;

public enum BillingFrequency
{
	Monthly,
	Yearly
}

public class PaymentInfoModel
{
	public int UserID { get; set; }

	public bool IsShippingSameAsBilling { get; set; }
	public bool SaveInfoForNextTime { get; set; }
	public string NameOnCard { get; set; } = string.Empty;
	public string CardNumber { get; set; } = string.Empty;
	public string ExpirationDate { get; set; } = string.Empty;
	public string CVV { get; set; } = string.Empty;
	public string BillingAddress { get; set; } = string.Empty;
	public string BillingCity { get; set; } = string.Empty;
	public string BillingState { get; set; } = string.Empty;
	public string BillingZipCode { get; set; } = string.Empty;

	public decimal ProductsTotal { get; set; }
	public decimal ShippingTotal { get; set; }
	public decimal TotalAmount { get; set; }
	public BillingFrequency BillingFrequency { get; set; }
	public DateTime StartDate { get; set; }
}
