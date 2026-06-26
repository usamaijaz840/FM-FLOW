using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace FMFlow.Entities;

public class Estimate
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int EstimateID { get; set; }

	[ForeignKey(nameof(Entities.RequestedEstimate))]
	public int RequestedEstimateID { get; set; }

	public virtual RequestedEstimate RequestedEstimate { get; set; } = null!;

	[ForeignKey(nameof(FlowUser))]
	public int ProUserID { get; set; }

	public virtual FlowUser ProUser { get; set; } = null!;

	[ForeignKey(nameof(ScheduledEstimate))]
	public int? ScheduledEstimateID { get; set; }

	public virtual ScheduledEstimate? ScheduledEstimate { get; set; }

	public EstimateStatus Status { get; set; }

	public DateTimeOffset StatusLastUpdate { get; set; } = DateTimeOffset.UtcNow;

	public JsonDocument? Attributes { get; set; }

	public JsonDocument? CalculationResults { get; set; }

	public decimal? Total { get; set; }

	public bool IsOnHold { get; set; } = false;

	public bool IsActive { get; set; } = true;

	public bool HasBeenPaid { get; set; } = false;

	public bool DepositHasBeenPaid { get; set; } = false;

	public decimal PaidAmount { get; set; } = 0;

	public virtual Job? Job { get; set; }

	[ForeignKey(nameof(Job))]
	public int? JobId { get; set; }

	public virtual ICollection<EstimateRecipient> EstimateRecipients { get; set; } = [];
}

public enum EstimateStatus
{
	ReadyToBeScheduled = -1, // estimates are never really in this state, but this is used for RequestedEstimates without Estimates
	Scheduled = 0,
	Approved = 1,
	InProgress = 2,
	Completed = 3,
	Canceled = 4,
}

public enum KanbanEstimateStatus
{
	OnHold = -1,
	ReadyToBeScheduled = 0,
	Scheduled = 1,
	Approved = 2,
	InProgress = 3,
	Completed = 4,
}
