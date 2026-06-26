using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FMFlow.Entities;

public class PaintSheenPrice
{
	[Key]
	public int PaintPriceId { get; set; }

	[ForeignKey(nameof(PaintSheen))]
	public int PaintSheenId { get; set; }

	public virtual PaintSheen PaintSheen { get; set; } = null!;

	[ForeignKey(nameof(FlowUser))]
	public int ProUserId { get; set; }

	public virtual FlowUser ProUser { get; set; } = null!;

	public decimal PricePerGallon { get; set; }

	[Required]
	public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;

	public DateTimeOffset? DateUpdated { get; set; }

	public bool IsDeleted { get; set; }

	public DateTimeOffset? DateDeleted { get; set; }
}
