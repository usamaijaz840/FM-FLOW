using FMFlow.Entities;
using FMFlow.ProUser.Interface.DTOs;
using Riok.Mapperly.Abstractions;

namespace FMFlow.ProUser.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, AllowNullPropertyAssignment = false)]
public partial class ProWeekDayAvailabilityMapper
{
	public ProWeekDayAvailability MapToWeekDayAvailabilityDto(
		WeekDayAvailabilityDTO dto,
		int userId)
	{
		return new ProWeekDayAvailability
		{
			DayOfWeek = dto.DayOfWeek,
			StartTime = dto.StartTime,
			EndTime = dto.EndTime,
			ProUserID = userId,
			DateUpdated = DateTime.UtcNow,
		};
	}
}
