using System.Text.Json;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using EFRepository;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Integrations.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FMFlow.Integrations.Service;

/// <summary>
/// Service for refreshing OAuth access tokens
/// </summary>
public class TokenRefreshService : ITokenRefreshService
{
	private readonly HttpClient _httpClient;
	private readonly GoogleSettings _googleSettings;
	private readonly OutlookSettings _outlookSettings;
	private readonly IRepository _repository;
	private readonly ILogger<TokenRefreshService> _logger;
	private readonly TimeSpan _refreshBuffer;

	// Ensures only one refresh occurs per integration at a time
	private static readonly ConcurrentDictionary<int, SemaphoreSlim> _refreshLocks = new();

	// Default: Refresh tokens 5 minutes before they expire to account for clock skew
	// Can be overridden via configuration for testing 
	private static readonly TimeSpan DefaultRefreshBuffer = TimeSpan.FromMinutes(5);

	public TokenRefreshService(
		HttpClient httpClient,
		IOptions<GoogleSettings> googleSettingsOptions,
		IOptions<OutlookSettings> outlookSettingsOptions,
		IRepository repository,
		IConfiguration configuration,
		ILogger<TokenRefreshService> logger)
	{
		_httpClient = httpClient;
		_googleSettings = googleSettingsOptions.Value;
		_outlookSettings = outlookSettingsOptions.Value;
		_repository = repository;
		_logger = logger;

		// Allow override for testing via appsettings or environment variable
		int? configSeconds = configuration.GetValue<int?>("IntegrationTokenRefresh:BufferSeconds");

		_refreshBuffer = configSeconds.HasValue
			? TimeSpan.FromSeconds(configSeconds.Value)
			: DefaultRefreshBuffer;

		if (_refreshBuffer != DefaultRefreshBuffer)
		{
			_logger.LogInformation("Token refresh buffer overridden to {BufferSeconds} seconds", _refreshBuffer.TotalSeconds);
		}
	}

	public async Task<Result<Integration>> EnsureValidTokenAsync(Integration integration, CancellationToken ct)
	{
		// Fast path: token still valid
		if (!IsTokenExpired(integration))
		{
			return Result<Integration>.Success(integration);
		}

		// Acquire per-integration lock to avoid concurrent refreshes
		var semaphore = _refreshLocks.GetOrAdd(integration.IntegrationID, _ => new SemaphoreSlim(1, 1));
		await semaphore.WaitAsync(ct);
		try
		{
			// Double-check after acquiring lock: reload latest integration
			var current = await _repository.Query<Integration>()
				.FirstOrDefaultAsync(i => i.IntegrationID == integration.IntegrationID, ct);

			if (current == null)
			{
				_logger.LogWarning("Integration {IntegrationId} not found during token refresh", integration.IntegrationID);
				return Result<Integration>.Failure("Integration not found.", ResultErrorType.NotFound);
			}

			if (!IsTokenExpired(current))
			{
				// Another request already refreshed it
				return Result<Integration>.Success(current);
			}

			if (string.IsNullOrEmpty(current.RefreshToken))
			{
				_logger.LogWarning(
					"Integration {IntegrationId} for user {UserId} has expired token but no refresh token",
					current.IntegrationID,
					current.UserID);
				return Result<Integration>.Failure(
					"Calendar integration has expired. Please reconnect your calendar.",
					ResultErrorType.Unauthorized);
			}

			// Refresh based on integration type
			try
			{
				return current.IntegrationType switch
				{
					IntegrationType.GoogleCalendar => await RefreshGoogleTokenAsync(current, ct),
					IntegrationType.OutlookCalendar => await RefreshOutlookTokenAsync(current, ct),
					_ => Result<Integration>.Failure(
						$"Unsupported integration type: {current.IntegrationType}",
						ResultErrorType.BadRequest)
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex,
					"Error refreshing token for integration {IntegrationId} user {UserId}",
					current.IntegrationID,
					current.UserID);
				return Result<Integration>.Failure(
					"Failed to refresh calendar access token. Please reconnect your calendar.",
					ResultErrorType.Unauthorized);
			}
		}
		finally
		{
			semaphore.Release();
		}
	}

	private bool IsTokenExpired(Integration integration)
	{
		if (integration.TokenExpiresAt == null)
		{
			// If we don't have expiration info, assume expired (legacy integrations)
			return true;
		}

		return DateTimeOffset.UtcNow >= integration.TokenExpiresAt.Value - _refreshBuffer;
	}

	private async Task<Result<Integration>> RefreshGoogleTokenAsync(Integration integration, CancellationToken ct)
	{
		using var content = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "client_id", _googleSettings.ClientId },
			{ "client_secret", _googleSettings.ClientSecret },
			{ "refresh_token", integration.RefreshToken! },
			{ "grant_type", "refresh_token" }
		});

		using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, _googleSettings.TokenApiUrl)
		{
			Content = content
		};

		using var response = await _httpClient.SendAsync(tokenRequest, ct);

		if (!response.IsSuccessStatusCode)
		{
			string errorContent = await response.Content.ReadAsStringAsync(ct);

			_logger.LogError(
				"Google token refresh failed with status {StatusCode}: {Error}",
				response.StatusCode,
				errorContent);

			return Result<Integration>.Failure(
				"Failed to refresh Google Calendar token. Please reconnect your calendar.",
				ResultErrorType.Unauthorized);
		}

		string tokenContent = await response.Content.ReadAsStringAsync(ct);
		JsonElement tokenJson = JsonDocument.Parse(tokenContent).RootElement;

		string? accessToken = tokenJson.TryGetProperty("access_token", out var accessTokenElement)
			? accessTokenElement.GetString() : null;

		if (accessToken == null)
		{
			return Result<Integration>.Failure(
				"Invalid response from Google token refresh",
				ResultErrorType.Unauthorized);
		}

		// Google refresh token response includes new access token and expiration, but not a new refresh token
		// (unless the refresh token was revoked, in which case we'd get an error above)
		int expiresInSeconds = tokenJson.TryGetProperty("expires_in", out var expiresInElement)
			? expiresInElement.GetInt32() : 3600;

		integration.Data = accessToken;
		integration.TokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);

		// Save updated integration
		_repository.AddOrUpdate(integration);
		await _repository.SaveAsync(ct);

		_logger.LogInformation(
			"Successfully refreshed Google Calendar token for integration {IntegrationId}",
			integration.IntegrationID);

		return Result<Integration>.Success(integration);
	}

	private async Task<Result<Integration>> RefreshOutlookTokenAsync(Integration integration, CancellationToken ct)
	{
		using var content = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "client_id", _outlookSettings.ClientId },
			{ "client_secret", _outlookSettings.ClientSecret },
			{ "refresh_token", integration.RefreshToken! },
			{ "grant_type", "refresh_token" },
			{ "scope", _outlookSettings.ScopeFields }
		});

		using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, _outlookSettings.GetTokenUrl())
		{
			Content = content
		};

		using var response = await _httpClient.SendAsync(tokenRequest, ct);

		if (!response.IsSuccessStatusCode)
		{
			var errorContent = await response.Content.ReadAsStringAsync(ct);
			_logger.LogError(
				"Outlook token refresh failed with status {StatusCode}: {Error}",
				response.StatusCode,
				errorContent);

			return Result<Integration>.Failure(
				"Failed to refresh Outlook Calendar token. Please reconnect your calendar.",
				ResultErrorType.Unauthorized);
		}

		string tokenContent = await response.Content.ReadAsStringAsync(ct);
		JsonElement tokenJson = JsonDocument.Parse(tokenContent).RootElement;

		string? accessToken = tokenJson.TryGetProperty("access_token", out var accessTokenElement)
			? accessTokenElement.GetString() : null;

		if (accessToken == null)
		{
			return Result<Integration>.Failure(
				"Invalid response from Outlook token refresh",
				ResultErrorType.Unauthorized);
		}

		// Outlook may return a new refresh token
		var newRefreshToken = tokenJson.TryGetProperty("refresh_token", out var refreshTokenElement)
			? refreshTokenElement.GetString()
			: null;

		var expiresInSeconds = tokenJson.TryGetProperty("expires_in", out var expiresInElement)
			? expiresInElement.GetInt32()
			: 3600;

		integration.Data = accessToken;
		integration.RefreshToken = newRefreshToken ?? integration.RefreshToken; // Keep existing if no new one
		integration.TokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);

		// Save updated integration
		_repository.AddOrUpdate(integration);
		await _repository.SaveAsync(ct);

		_logger.LogInformation(
			"Successfully refreshed Outlook Calendar token for integration {IntegrationId}",
			integration.IntegrationID);

		return Result<Integration>.Success(integration);
	}
}
