using EFRepository;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using FMFlow.Integrations.Interface.DTOs;
using FMFlow.Integrations.Interface;
using FMFlow.Entities;
using FMFlow.Identity.Interface;
using FMFlow.FlowAPI.Interface;
using Microsoft.Extensions.Options;
using FMFlow.Common;
using System.IdentityModel.Tokens.Jwt;
using FMFlow.Common.Services;
using Microsoft.Extensions.Logging;

namespace FMFlow.Integrations.Service;

public class IntegrationsService : IIntegrationsService
{
	private readonly GoogleSettings _googleSettings;
	private readonly OutlookSettings _outlookSettings;
	private readonly AppSettings _appSettings;
	private readonly IRepository _repository;
	private readonly ICurrentUserService _currentUserService;
	private readonly INonceService _nonceService;
	private readonly IApiUrlBuilder _apiUrlBuilder;
	private readonly HttpClient _httpClient;
	private readonly ILogger<IntegrationsService> _logger;

	private string GoogleRedirectUri => _apiUrlBuilder.GetFullUrl("/api/Integrations/GoogleCalendar");
	private string OutlookRedirectUri => _apiUrlBuilder.GetFullUrl("/api/Integrations/OutlookCalendar");

	public IntegrationsService(
	IOptions<GoogleSettings> googleSettingsOptions,
	IOptions<OutlookSettings> outlookSettingsOptions,
	IOptions<AppSettings> appSettingsOptions,
	IRepository repository,
	ICurrentUserService currentUserService,
	INonceService nonceService,
	IApiUrlBuilder urlBuilder,
	HttpClient httpClient,
	ILogger<IntegrationsService> logger)
	{
		_googleSettings = googleSettingsOptions.Value;
		_outlookSettings = outlookSettingsOptions.Value;
		_appSettings = appSettingsOptions.Value;
		_repository = repository;
		_currentUserService = currentUserService;
		_nonceService = nonceService;
		_apiUrlBuilder = urlBuilder;
		_httpClient = httpClient;
		_logger = logger;
	}

	public async Task<Result<List<IntegrationResponseDto>>> GetIntegrations(CancellationToken ct)
	{
		var userId = _currentUserService.GetUserID();
		if (userId == null)
		{
			return Result<List<IntegrationResponseDto>>.Success([]);
		}
		var result = await _repository.Query<Integration>()
			.ByUserID(userId)
			.AsNoTracking()
			.ToListAsync(ct) ?? [];
		var dtos = result.Select(row => new IntegrationResponseDto(row.IntegrationID, row.IntegrationType)).ToList();
		return Result<List<IntegrationResponseDto>>.Success(dtos);
	}

	// Process the Google OAuth code, exchange it for an access token and save it
	private async Task<Result<string?>> SaveGoogleCalendarIntegration(string code, string state, CancellationToken ct)
	{
		// Get userId from state (nonce)
		Result<Nonce> nonceValidationResult = await _nonceService.ValidateAndConsumeNonce(state, ct);
		if (!nonceValidationResult.IsSuccess)
			return Result<string?>.Failure(nonceValidationResult.Error!, nonceValidationResult.ErrorType);

		int userId = nonceValidationResult.Value!.EntityId;

		// Check if user with userId has existing Outlook integration (can only have one type of calendar integration)
		Integration? existingOutlookIntegration = await _repository.Query<Integration>()
		 	.ByUserID(userId)
		 	.Where(r => r.IntegrationType == IntegrationType.OutlookCalendar)
		 	.FirstOrDefaultAsync(ct);

		if (existingOutlookIntegration != null)
			return Result<string?>.Failure("User already has an Outlook integration", ResultErrorType.BadRequest);

		// Exchange the authorization code for an access token
		string googleTokenUrl = _googleSettings.TokenApiUrl;
		string clientId = _googleSettings.ClientId;
		string clientSecret = _googleSettings.ClientSecret;

		var tokenRequest = new HttpRequestMessage(HttpMethod.Post, googleTokenUrl)
		{
			Content = new FormUrlEncodedContent(new Dictionary<string, string>
			{
			{ "code", code },
			{ "client_id", clientId },
			{ "client_secret", clientSecret },
			{ "redirect_uri", GoogleRedirectUri },
			{ "grant_type", "authorization_code" },
				{ "state", state }
			})
		};

		HttpResponseMessage tokenResponse = await _httpClient.SendAsync(tokenRequest, ct);
		tokenResponse.EnsureSuccessStatusCode();

		string tokenContent = await tokenResponse.Content.ReadAsStringAsync();
		JsonElement tokenJson = JsonDocument.Parse(tokenContent).RootElement;
		string? accessToken = tokenJson.GetProperty("access_token").GetString();

		if (accessToken == null)
			return Result<string?>.Failure("Failed to retrieve access token from Google.", ResultErrorType.BadRequest);

		// Extract refresh token and expiration info
		string? refreshToken = tokenJson.TryGetProperty("refresh_token", out var refreshTokenElement) 
			? refreshTokenElement.GetString() 
			: null;
		
		int expiresInSeconds = tokenJson.TryGetProperty("expires_in", out var expiresInElement) 
			? expiresInElement.GetInt32() 
			: 3600; // Default to 1 hour
		
		DateTimeOffset tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);

		// Get user email from id_token (JWT) included in the token response
		string? userEmail = null;
		if (tokenJson.TryGetProperty("id_token", out var idTokenElement))
		{
			string? idToken = idTokenElement.GetString();
			if (idToken != null)
			{
				var handler = new JwtSecurityTokenHandler();
				var jwtToken = handler.ReadJwtToken(idToken);
				userEmail = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
			}
		}

		// Check if this token already has calendar scope (second OAuth callback)
		string? scope = tokenJson.TryGetProperty("scope", out var scopeElement) ? scopeElement.GetString() : null;
		if (scope != null && scope.Contains("calendar.events.freebusy"))
		{
			// Calendar scope present - save integration and we're done
			if (string.IsNullOrEmpty(refreshToken))
			{
				_logger.LogWarning(
					"Google OAuth for user {UserId} did not return a refresh token at calendar-scope step. Integration will expire when access token does.",
					userId);
			}
			await UpsertIntegration(IntegrationType.GoogleCalendar, accessToken!, userId, refreshToken, tokenExpiresAt, ct);
			return Result<string?>.Success(null);
		}

		// Calendar scope missing - this is the first OAuth callback
		// Generate nonce for second OAuth step
		Result<string> calendarNonceResult = await _nonceService.GenerateAndSaveNonce(userId, NonceType.HandleGoogleIntegration, ct);
		if (!calendarNonceResult.IsSuccess)
			return Result<string?>.Failure(calendarNonceResult.Error!, calendarNonceResult.ErrorType);

		// Return redirect URL as success value (reuse googleRedirectUri from earlier)
		string calendarAuthUrl = _googleSettings.GetCalendarAuthUrl(calendarNonceResult.Value!, GoogleRedirectUri, userEmail ?? "");
		if (string.IsNullOrEmpty(refreshToken))
		{
			_logger.LogWarning(
				"Google OAuth for user {UserId} did not include a refresh token on the initial step. Continuing to calendar consent step.",
				userId);
		}
		return Result<string?>.Success(calendarAuthUrl);
	}

	// Process the Outlook OAuth code, exchange it for an access token and save it
	private async Task<Result> SaveOutlookCalendarIntegration(string code, string state, CancellationToken ct)
	{
		// Get userId from state (nonce)
		Result<Nonce> nonceValidationResult = await _nonceService.ValidateAndConsumeNonce(state, ct);
		if (!nonceValidationResult.IsSuccess)
			return nonceValidationResult;

		int userId = nonceValidationResult.Value!.EntityId;

		// Check if user with userId has existing integration (can only have one type of calendar integration)
		Integration? existsGoogleIntegration = await _repository.Query<Integration>()
		 	.ByUserID(userId)
		 	.Where(r => r.IntegrationType == IntegrationType.GoogleCalendar)
		 	.FirstOrDefaultAsync(ct);

		if (existsGoogleIntegration != null)
			return Result.Failure("User already has a Google integration", ResultErrorType.BadRequest);

		// Exchange the authorization code for an access token
		string outlookTokenUrl = _outlookSettings.GetTokenUrl();
		string clientId = _outlookSettings.ClientId;
		string clientSecret = _outlookSettings.ClientSecret;
		string scope = _outlookSettings.ScopeFields;

		var tokenRequest = new HttpRequestMessage(HttpMethod.Post, outlookTokenUrl)
		{
			Content = new FormUrlEncodedContent(new Dictionary<string, string>
			{
			{ "code", code },
			{ "client_id", clientId },
			{ "client_secret", clientSecret },
			{ "redirect_uri", OutlookRedirectUri },
			{ "scope", scope },
				{ "grant_type", "authorization_code" },
				{ "state", state }
			})
		};

		HttpResponseMessage tokenResponse = await _httpClient.SendAsync(tokenRequest, ct);
		tokenResponse.EnsureSuccessStatusCode();

		string tokenContent = await tokenResponse.Content.ReadAsStringAsync();
		JsonElement tokenJson = JsonDocument.Parse(tokenContent).RootElement;
		string? accessToken = tokenJson.GetProperty("access_token").GetString();

		if (accessToken == null)
			return Result.Failure("Failed to retrieve access token from Outlook.", ResultErrorType.BadRequest);

		// Extract refresh token and expiration info
		string? refreshToken = tokenJson.TryGetProperty("refresh_token", out var refreshTokenElement) 
			? refreshTokenElement.GetString() 
			: null;
		
		if (string.IsNullOrEmpty(refreshToken))
		{
			_logger.LogWarning(
				"Outlook OAuth for user {UserId} did not return a refresh token. This typically happens when the user has previously authorized the app. " +
				"The integration will become unusable after the access token expires.",
				userId);
			return Result.Failure(
				"Could not connect to Outlook Calendar. Please try disconnecting and reconnecting your Outlook account.",
				ResultErrorType.BadRequest);
		}
		
		int expiresInSeconds = tokenJson.TryGetProperty("expires_in", out var expiresInElement) 
			? expiresInElement.GetInt32() 
			: 3600; // Default to 1 hour
		
		DateTimeOffset tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);

		// Upsert integration
		await UpsertIntegration(IntegrationType.OutlookCalendar, accessToken, userId, refreshToken, tokenExpiresAt, ct);

		return Result.Success();
	}

	private async Task UpsertIntegration(IntegrationType integrationType, string data, int userId, string? refreshToken, DateTimeOffset tokenExpiresAt, CancellationToken ct)
	{
		var existingIntegration = await _repository.Query<Integration>()
					 .ByUserID(userId)
					 .Where(r => r.IntegrationType == integrationType)
					 .FirstOrDefaultAsync(ct);

		if (existingIntegration == null)
		{
			var integration = new Integration
			{
				UserID = userId,
				IntegrationType = integrationType,
				Data = data,
				RefreshToken = refreshToken,
				TokenExpiresAt = tokenExpiresAt
			};

			_repository.AddNew(integration);
		}
		else
		{
			existingIntegration.Data = data;
			existingIntegration.RefreshToken = refreshToken ?? existingIntegration.RefreshToken; // Keep existing if new one is null
			existingIntegration.TokenExpiresAt = tokenExpiresAt;
			_repository.AddOrUpdate(existingIntegration);
		}

		await _repository.SaveAsync(ct);
	}

	public async Task<Result> DeleteIntegration(IntegrationType type, CancellationToken ct)
	{
		var userId = _currentUserService?.GetUserID();
		var integration = await _repository.Query<Integration>()
			.FirstOrDefaultAsync(x => x.UserID == userId && x.IntegrationType == type, ct);

		if (integration == null)
			return Result.Failure("Integration not found.", ResultErrorType.NotFound);

		_repository.Delete(integration);
		await _repository.SaveAsync(ct);

		return Result.Success();
	}

	public async Task<Result<string?>> HandleOutlookCalendarIntegration(string? code, string? state, CancellationToken ct)
	{
		if (string.IsNullOrEmpty(state))
			return Result<string?>.Failure("Missing state parameter", ResultErrorType.BadRequest);

		if (string.IsNullOrEmpty(code))
		{
			// Chain the nonce validation and generation operations to get OAuth URL
			Result<string?> validationResult = await _nonceService.ValidateAndConsumeNonce(state, ct)
				.MapResult(async (nonce, ct) => await _nonceService.GenerateAndSaveNonce(nonce.EntityId, NonceType.HandleOutlookIntegration, ct), ct)
				.MapResult(newState => Result<string?>.Success(_outlookSettings.GetAuthUrl(newState, OutlookRedirectUri)), ct);

			return validationResult;
		}

		// Auth code is present: exchange it for a token and store integration
		Result<string?> authResult = await SaveOutlookCalendarIntegration(code, state, ct)
			.MapResult(() => Result<string?>.Success(_appSettings.Frontend), ct);

		return authResult;
	}

	public async Task<Result<string?>> HandleGoogleCalendarIntegration(string? code, string? state, CancellationToken ct)
	{
		if (string.IsNullOrEmpty(state))
			return Result<string?>.Failure("Missing state parameter", ResultErrorType.BadRequest);

		if (string.IsNullOrEmpty(code))
		{
			Result<string?> validationResult = await _nonceService.ValidateAndConsumeNonce(state, ct)
				.MapResult(async (nonce, ct) => await _nonceService.GenerateAndSaveNonce(nonce.EntityId, NonceType.HandleGoogleIntegration, ct), ct)
				.MapResult(newState => Result<string?>.Success(_googleSettings.GetAuthUrl(newState, GoogleRedirectUri)), ct);

			return validationResult;
		}

		// Auth code is present: exchange it for a token and store integration
		Result<string?> integrationResult = await SaveGoogleCalendarIntegration(code, state, ct);

		if (!integrationResult.IsSuccess)
			return integrationResult;

		// Check if a redirect URL was returned (calendar scope missing)
		if (integrationResult.Value != null)
			return Result<string?>.Success(integrationResult.Value);

		// Integration complete, redirect to frontend
		return Result<string?>.Success(_appSettings.Frontend);
	}

	public async Task<Result<string>> VerifyIntegrationStatus(IntegrationType? integrationType, CancellationToken ct)
	{
		if (integrationType == null)
		{
			integrationType = IntegrationType.GoogleCalendar;
		}

		var integrations = await GetIntegrations(ct);
		if (integrations.Value != null && integrations.Value.Count > 0)
		{
			if (integrationType == IntegrationType.GoogleCalendar)
			{
				var integrationExists = integrations.Value.Exists(i => i.IntegrationType == IntegrationType.OutlookCalendar);
				if (integrationExists)
				{
					return Result<string>.Failure("You've already connected your Outlook account.", ResultErrorType.BadRequest);
				}
			}
			else
			{
				var integrationExists = integrations.Value.Exists(i => i.IntegrationType == IntegrationType.GoogleCalendar);
				if (integrationExists)
				{
					return Result<string>.Failure("You've already connected your Google account.", ResultErrorType.BadRequest);
				}
			}
		}

		// Generate nonce, save to DB, and return
		int userId = _currentUserService.GetUserID();
		Result<string> result = await _nonceService.GenerateAndSaveNonce(userId, NonceType.VerifyIntegrationStatus, ct);

		return result;
	}
}

