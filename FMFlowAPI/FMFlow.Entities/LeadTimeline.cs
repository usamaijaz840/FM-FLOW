using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace FMFlow.Entities;

public class LeadTimeline
{
	[Key]
	public int TimelineId { get; set; }

	[Required]
	[ForeignKey(nameof(Lead))]
	public int LeadId { get; set; }

	public virtual Lead Lead { get; set; } = null!;

	[ForeignKey(nameof(FlowUser))]
	public int UserId { get; set; }

	public virtual FlowUser User { get; set; } = null!;

	[Required]
	public string EventNameKey { get; set; } = null!;

	[Required]
	public string EventKey { get; set; }

	[Required]
	public JsonDocument EventParameters { get; set; } = null!;

	public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;
}

public enum TimelineEventKey
{
	LeadCreated,
	EstimateSent,
	EstimateScheduled,
	EstimateInProgress,
	EstimateOnHold,
	EstimateCanceled,
	EstimateApproved,
	JobScheduled,
	JobOnHold,
	JobClosed
}
