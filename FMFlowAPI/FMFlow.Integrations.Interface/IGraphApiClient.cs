using Microsoft.Graph.Models;

namespace FMFlow.Integrations.Interface;

/// <summary>
/// Abstraction for Microsoft Graph API operations to improve testability
/// </summary>
public interface IGraphApiClient
{
    /// <summary>
    /// Gets calendar events for the authenticated user within the specified date range
    /// </summary>
    /// <param name="accessToken">OAuth access token for authentication</param>
    /// <param name="startTime">Start date/time for the query</param>
    /// <param name="endTime">End date/time for the query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of calendar events</returns>
    Task<IEnumerable<Event>> GetCalendarEventsAsync(string accessToken, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);
}