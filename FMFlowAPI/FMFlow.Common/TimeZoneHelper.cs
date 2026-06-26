namespace FMFlow.Common;

/// <summary>
/// Provides helpers to impersonate a time zone (so the wall time will be preserved, while the offset changes to the target time zone).
///
/// Used to display the times from the original time zone as if they were in the current user's time zone.
/// </summary>
public static class TimeZoneHelper
{
	/// <summary>
	/// Updates the offset of the original time while preserving the rest.
	///
	/// For example, 10:00 AM in MST could be changed to 10:00 AM in CST.
	///
	/// WARNING: This should only be used if you know you need it. In general,
	/// it is better to use built-in functions to convert between time zones.
	/// </summary>
	/// <param name="originalTime">The real time that needs to be impersonated.</param>
	/// <param name="originalTimeZone">The time zone for the original time.</param>
	/// <param name="targetTimeZone">The time zone to which the original time should be moved.</param>
	/// <returns>The original time in a different time zone.</returns>
	public static DateTimeOffset PreserveTimeInDifferentTimeZone(DateTimeOffset originalTime, TimeZoneInfo originalTimeZone, TimeZoneInfo targetTimeZone)
	{
		var originalTimeInZone = TimeZoneInfo.ConvertTime(originalTime, originalTimeZone);
		return new DateTimeOffset(originalTimeInZone.DateTime, targetTimeZone.GetUtcOffset(originalTimeInZone.DateTime));
	}
}
