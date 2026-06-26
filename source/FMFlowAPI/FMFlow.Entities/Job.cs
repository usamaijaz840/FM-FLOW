using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FMFlow.Entities;

public class Job
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int JobId { get; set; }

	[ForeignKey(nameof(Estimate))]
	public int EstimateId { get; set; }

	public virtual Estimate Estimate { get; set; } = null!;

	public JobStatus Status { get; set; } = JobStatus.Scheduled;

	public string? Summary { get; set; }

	public bool IsOnHold { get; set; } = false;

	public bool IsActive { get; set; } = true;

	public DateTimeOffset ScheduledDateWorkStarted { get; set; }

	public DateOnly ScheduledDateWorkCompleted { get; set; }

	public DateOnly? ActualDateWorkStarted { get; set; }

	public DateOnly? ActualDateWorkCompleted { get; set; }

	public DateOnly? SignOffDate { get; set; }

	public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;

	public DateTimeOffset? DateDeleted { get; set; }

	public DateTimeOffset StatusLastUpdate { get; set; } = DateTimeOffset.UtcNow;

	public bool IsDeleted { get; set; } = false;

	public bool? NotifiedPriorToArrival { get; set; }

	public bool? CooperativeWithCustomer { get; set; }

	public bool? CleanedUpWorkAreas { get; set; }

	public bool? CompletedScopeOfWork { get; set; }

	public bool? ContractorWorkIsSatisfactory { get; set; }

	public JobRateContractorPerformance? RateContractorPerformance { get; set; }

	public string? SignOffComment { get; set; }
}

public enum JobStatus
{
	Scheduled = 0,
	InProgress = 1,
	PendingCompletion = 3,
	Closed = 4
}

public enum KanbanJobStatus
{
	OnHold = -1,
	Scheduled = 0,
	InProgress = 1,
	PendingCompletion = 3,
	Closed = 4
}

public enum JobRateContractorPerformance
{
	Excellent = 0,
	Good = 1,
	Poor = 2
}
