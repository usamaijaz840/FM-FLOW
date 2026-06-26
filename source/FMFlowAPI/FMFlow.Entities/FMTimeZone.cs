using System.ComponentModel.DataAnnotations;

namespace FMFlow.Entities;

public class FMTimeZone // FM preffix to avoid conflict with System.TimeZone
{
	[Key]
	public int TimeZoneId { get; set; }

	public string Name { get; set; } = string.Empty; // Display name for the time zone
	
	public string SystemTimeZoneId { get; set; } = string.Empty; // C# system time zone identifier
}
