using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FMFlow.Entities;

public class CustomerTempPro
{
	[Key]
	public int CustomerTempProId { get; set; }

	public DateTimeOffset ExpireDateTime { get; set; } = DateTimeOffset.UtcNow.AddHours(2);

	[Required]
	[ForeignKey(nameof(Customer))]
	public int CustomerId { get; set; }
	public virtual FlowUser Customer { get; set; } = null!;

	[Required]
	[ForeignKey(nameof(Pro))]
	public int ProId { get; set; }
	public virtual FlowUser Pro { get; set; } = null!;
}
