using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FMFlow.Entities;

public class PaintSheen
{
	[Key]
	public int PaintSheenId { get; set; }

	[ForeignKey(nameof(Sheen))]
	public int SheenId { get; set; }

	public virtual Sheen Sheen { get; set; } = null!;

	[ForeignKey(nameof(Paint))]
	public int PaintId { get; set; }

	public virtual Paint Paint { get; set; } = null!;

	public virtual ICollection<PaintSheenPrice> PaintSheenPrices { get; set; } = new List<PaintSheenPrice>();

	[Required]
	public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;

	public DateTimeOffset? DateUpdated { get; set; }

	public bool IsDeleted { get; set; }

	public DateTimeOffset? DateDeleted { get; set; }
}
