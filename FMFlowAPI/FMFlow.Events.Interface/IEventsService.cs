using FMFlow.Events.Interface.DTOs;
using FMFlow.FlowAPI.Interface;

namespace FMFlow.Events.Service
{
    public interface IEventsService
    {
		Task<Result<ProEventsDto>> GetProEvents(DateOnly startDate, DateOnly endDate, int? proId, int? projectId, string? userTimeZone, CancellationToken ct, bool isMonthView = false, string? sessionId = null);
    }
}
