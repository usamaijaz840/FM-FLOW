using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EFRepository;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Identity.Interface;
using FMFlow.Integrations.Interface;
using FMFlow.Integrations.Interface.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FMFlow.Integrations.Service;

public class GoogleCalendarService(
	IRepository repository,
	ICurrentUserService currentUserService,
	HttpClient httpClient,
	IOptions<GoogleSettings> settingsOptions,
	ITokenRefreshService tokenRefreshService,
	ILogger<GoogleCalendarService> logger) : ICalendarService
{
	private readonly GoogleSettings _settings = settingsOptions.Value;

	public async Task<bool> IsCalendarConnected(CancellationToken ct, int? proId = null)
	{
		var currentProId = proId ?? currentUserService.GetUserID();

		var integration = await repository.Query<Integration>()
			.ByUserID(currentProId)
			.Where(r => r.IntegrationType == IntegrationType.GoogleCalendar)
			.FirstOrDefaultAsync(ct);
		return integration != null;
	}

	public async Task<List<BusyTime>> GetBusyTimes(DateTime timeMin, DateTime timeMax, CancellationToken ct, int? proId = null)
	{
		var currentProId = proId ?? currentUserService.GetUserID();

		var integration = await repository.Query<Integration>()
			.ByUserID(currentProId)
			.Where(r => r.IntegrationType == IntegrationType.GoogleCalendar)
			.FirstOrDefaultAsync(ct);

		if (integration == null)
			return new List<BusyTime>();

		// Ensure token is valid, refresh if necessary
		Result<Integration> tokenResult = await tokenRefreshService.EnsureValidTokenAsync(integration, ct);
		if (!tokenResult.IsSuccess)
		{
			logger.LogWarning(
				"Failed to ensure valid token for user {UserId}: {Error}",
				currentProId,
				tokenResult.Error);
			return [];
		}

		integration = tokenResult.Value!;

		var url = _settings.CalendarApiUrl;
		string calendarId = "primary";

		httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", integration.Data);

		var requestPayload = new
		{
			timeMin = timeMin.ToString("o"),
			timeMax = timeMax.ToString("o"),
			items = new[] { new { id = calendarId } }
		};

		var content = new StringContent(JsonSerializer.Serialize(requestPayload), Encoding.UTF8, "application/json");

		var response = await httpClient.PostAsync(url, content, ct);

		if (!response.IsSuccessStatusCode)
		{
			logger.LogError("Google Calendar API error: {StatusCode}", response.StatusCode);
			return [];
		}

		string json = await response.Content.ReadAsStringAsync(ct);

		var result = JsonSerializer.Deserialize<GoogleFreeBusyResponseDto>(json, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		});

		return result?.Calendars[calendarId].Busy ?? new List<BusyTime>();
	}
}