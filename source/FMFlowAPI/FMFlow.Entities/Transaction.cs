using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FMFlow.Entities;

public class Transaction
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int TransactionID { get; set; }

	// MX transaction Id?

	[ForeignKey(nameof(Estimate))]
	public int? EstimateId { get; set; }

	public virtual Estimate? Estimate { get; set; }

	public decimal Credit { get; set; }

	public decimal Debit { get; set; }

	public string Description { get; set; } = string.Empty;

	public PaymentMethod PaymentMethod { get; set; }

	public DateTimeOffset PaymentDate { get; set; } = DateTimeOffset.UtcNow;

	// Named as TxStatus Instead of Transaction status to avoid ambiguious reference with
	// .NET's built-in type System.Transactions.TransactionStatus
	public TxStatus Status { get; set; }

	// Additional error details from payment processor
	public string? ResponseMessage { get; set; }

	public string? AuthMessage { get; set; }
}

public enum PaymentMethod
{
	ACH,
	CC,
	Manual,
	Discount
}

public enum TxStatus
{
	Declined,
	Approved,
	Settled,
	Unknown
}
