using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FMFlow.Entities.ModelInterfaces;

namespace FMFlow.Entities;

public class Sheen : IHasNameProperty, IHasDateChangeTracking, IHasDeleted
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int SheenID { get; set; }

	public string Name { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	[ForeignKey(nameof(FlowUser))]
	public int? UserId { get; set; }

	public virtual FlowUser? User { get; set; }

	[Required]
	public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;

	public DateTimeOffset? DateUpdated { get; set; }

	public bool IsDeleted { get; set; }

	public DateTimeOffset? DateDeleted { get; set; }
}
