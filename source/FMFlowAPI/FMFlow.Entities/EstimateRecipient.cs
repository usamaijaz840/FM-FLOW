using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FMFlow.Entities.ModelInterfaces;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.Entities;

[Table("EstimateRecipients")]
[Index(nameof(EstimateId), nameof(RecipientEmail), IsUnique = true)]
public class EstimateRecipient : IHasDateChangeTracking, IHasDeleted
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int EstimateRecipientId { get; set; }

	[Required]
	[ForeignKey(nameof(Estimate))]
	public int EstimateId { get; set; }

	public virtual Estimate Estimate { get; set; } = null!;

	[Required]
	public string RecipientEmail { get; set; } = string.Empty;

	public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;

	public DateTimeOffset? DateUpdated { get; set; }

	public DateTimeOffset? DateDeleted { get; set; }

	public bool IsDeleted { get; set; }
}
