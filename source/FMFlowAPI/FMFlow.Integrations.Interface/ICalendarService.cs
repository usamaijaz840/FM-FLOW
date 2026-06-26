using FMFlow.Integrations.Interface.DTOs;

namespace FMFlow.Integrations.Interface;

public interface ICalendarService
{
	Task<bool> IsCalendarConnected(CancellationToken ct, int? proId = null);
	Task<List<BusyTime>> GetBusyTimes(DateTime timeMin, DateTime timeMax, CancellationToken ct, int? proId = null);
}
