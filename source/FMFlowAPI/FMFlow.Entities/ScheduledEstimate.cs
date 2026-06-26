using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FMFlow.Entities.ModelInterfaces;

namespace FMFlow.Entities;

public class ScheduledEstimate : IHasDateChangeTracking, IHasDeleted
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int ScheduledEstimateID { get; set; }

	[ForeignKey(nameof(FlowUser))]
	public int ProUserID { get; set; }

	public virtual FlowUser ProUser { get; set; }

	[ForeignKey(nameof(Project))]
	public int ProjectID { get; set; }

	public virtual Project Project { get; set; }

	[Required]
	public DateTimeOffset ScheduledDateTime { get; set; }

	[Required]
	public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;

	public DateTimeOffset? DateUpdated { get; set; }

	public bool IsDeleted { get; set; }

	public DateTimeOffset? DateDeleted { get; set; }

	public virtual List<Estimate> Estimates { get; set; } = [];
}
