using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FMFlow.Entities;

public class ProWeekDayAvailability
{
	[Key]
	public int ProWeekDayAvailabilityID { get; set; }

	[ForeignKey(nameof(FlowUser))]
	public int ProUserID { get; set; }

	public virtual FlowUser ProUser { get; set; } = null!;

	public DayOfWeek DayOfWeek { get; set; }

	public TimeOnly StartTime { get; set; }

	public TimeOnly EndTime { get; set; }

	public DateTimeOffset? DateUpdated { get; set; }
}
