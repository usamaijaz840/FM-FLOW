using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FMFlow.Entities.ModelInterfaces;

namespace FMFlow.Entities;

public class RequestedEstimate : IHasDateChangeTracking, IHasDeleted
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int RequestedEstimateID { get; set; }

	[ForeignKey(nameof(Project))]
	public int ProjectID { get; set; }

	public virtual Project Project { get; set; } = null!;

	[ForeignKey(nameof(EstimateType))]
	public int EstimateTypeId { get; set; }

	public virtual EstimateType EstimateType { get; set; } = null!;

	[ForeignKey(nameof(FlowUser))]
	public int? ProUserID { get; set; } // For requested estimates, this means the pro user CREATED it

	public virtual FlowUser? ProUser { get; set; }

	[Required]
	public string Name { get; set; } = null!;

	public bool IsChangeOrder { get; set; } = false;

	[Required]
	public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;

	public DateTimeOffset? DateUpdated { get; set; }

	public bool IsDeleted { get; set; }

	public DateTimeOffset? DateDeleted { get; set; }

	public List<Estimate> Estimates { get; set; } = [];
}
