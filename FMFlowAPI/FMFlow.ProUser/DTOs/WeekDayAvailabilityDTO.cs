namespace FMFlow.ProUser.Interface.DTOs;

public class WeekDayAvailabilityDTO
{
	public DayOfWeek DayOfWeek { get; set; }

	public TimeOnly StartTime { get; set; }

	public TimeOnly EndTime { get; set; }
}
