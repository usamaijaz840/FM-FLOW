using EFRepository;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Identity.Interface;
using FMFlow.Integrations.Interface;
using FMFlow.Integrations.Interface.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Models;
using System.Globalization;

namespace FMFlow.Integrations.Service;

public class OutlookCalendarService : ICalendarService
{
	private readonly IRepository _repository;
	private readonly ICurrentUserService _currentUserService;
	private readonly IGraphApiClient _graphApiClient;
	private readonly OutlookSettings _settings;
	private readonly ITokenRefreshService _tokenRefreshService;
	private readonly ILogger<OutlookCalendarService> _logger;

	public OutlookCalendarService(
		IRepository repository,
		ICurrentUserService currentUserService,
		IGraphApiClient graphApiClient,
		IOptions<OutlookSettings> settingsOptions,
		ITokenRefreshService tokenRefreshService,
		ILogger<OutlookCalendarService> logger)
	{
		_repository = repository ?? throw new ArgumentNullException(nameof(repository));
		_currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
		_graphApiClient = graphApiClient ?? throw new ArgumentNullException(nameof(graphApiClient));
		_settings = settingsOptions?.Value ?? throw new ArgumentNullException(nameof(settingsOptions));
		_tokenRefreshService = tokenRefreshService ?? throw new ArgumentNullException(nameof(tokenRefreshService));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<bool> IsCalendarConnected(CancellationToken ct, int? proId = null)
	{
		var currentProId = proId ?? _currentUserService.GetUserID();

		var integration = await _repository.Query<Integration>()
			.ByUserID(currentProId)
			.Where(r => r.IntegrationType == IntegrationType.OutlookCalendar)
			.FirstOrDefaultAsync(ct);
		return integration != null;
	}


	public async Task<List<BusyTime>> GetBusyTimes(DateTime timeMin, DateTime timeMax, CancellationToken ct, int? proId = null)
	{
		var currentProId = proId ?? _currentUserService.GetUserID();

		var integration = await _repository.Query<Integration>()
			.ByUserID(currentProId)
			.Where(r => r.IntegrationType == IntegrationType.OutlookCalendar)
			.FirstOrDefaultAsync(ct);

		if (integration == null)
			return new List<BusyTime>();

		try
		{
			// Ensure token is valid, refresh if necessary
			Result<Integration> tokenResult = await _tokenRefreshService.EnsureValidTokenAsync(integration, ct);

			if (!tokenResult.IsSuccess)
			{
				_logger.LogWarning(
					"Failed to ensure valid token for user {UserId}: {Error}",
					currentProId,
					tokenResult.Error);
				return new List<BusyTime>();
			}

			integration = tokenResult.Value!;

			var events = await _graphApiClient.GetCalendarEventsAsync(integration.Data, timeMin, timeMax, ct);

			return events
				.Where(e => e.ShowAs == FreeBusyStatus.Busy || e.ShowAs == FreeBusyStatus.WorkingElsewhere)
				.Select(e => new BusyTime
				{
					Start = DateTime.Parse(e.Start.DateTime, null, DateTimeStyles.RoundtripKind),
					End = DateTime.Parse(e.End.DateTime, null, DateTimeStyles.RoundtripKind),
					TimeZone = e.Start.TimeZone
				})
				.ToList();
		}
		catch (Exception ex)
		{
			// Log the error but don't fail the entire request - just return empty busy times
			// This allows the schedule page to load even if there's an issue with the Outlook integration
			_logger.LogError(ex, "Error retrieving Outlook calendar busy times for user {UserId}", currentProId);
			return new List<BusyTime>();
		}
	}
}
