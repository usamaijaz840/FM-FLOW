
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.ProUser.Interface.DTOs;

namespace FMFlow.ProUser;

public interface IProWeekDayAvailabilitiesService
{
	Task<List<ProWeekDayAvailability>> GetWeekDayAvailabilities(int userID, CancellationToken ct);
	Task<Result> UpdateWeekDayAvailabilities(int userID, WeekDayAvailabilityDTO[] availabilities, CancellationToken ct);
}
