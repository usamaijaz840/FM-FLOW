using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;
using FMFlow.Integrations.Interface;

namespace FMFlow.Integrations.Service;

/// <summary>
/// Implementation of Microsoft Graph API client using the Graph SDK
/// </summary>
public class GraphApiClient : IGraphApiClient
{
    private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffK";

    public async Task<IEnumerable<Event>> GetCalendarEventsAsync(string accessToken, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        var graphClient = CreateGraphServiceClient(accessToken);
        
        var events = await graphClient.Me.CalendarView.GetAsync(requestConfiguration =>
        {
            requestConfiguration.QueryParameters.StartDateTime = startTime.ToString(DateTimeFormat);
            requestConfiguration.QueryParameters.EndDateTime = endTime.ToString(DateTimeFormat);
        }, cancellationToken);

        return events?.Value ?? new List<Event>();
    }

    private GraphServiceClient CreateGraphServiceClient(string accessToken)
    {
        var authProvider = new BaseBearerTokenAuthenticationProvider(new TokenProvider(accessToken));
        return new GraphServiceClient(authProvider);
    }

    /// <summary>
    /// Simple token provider that returns the provided access token
    /// </summary>
    private class TokenProvider : IAccessTokenProvider
    {
        private readonly string _accessToken;

        public TokenProvider(string accessToken)
        {
            _accessToken = accessToken;
        }

        public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_accessToken);
        }

        public AllowedHostsValidator AllowedHostsValidator { get; } = new AllowedHostsValidator();
    }
}