using FMFlow.Common.Extensions;

namespace FMFlow.Integrations.Service;

public class GoogleSettings
{
	public const string SectionName = "Google";
	public string ClientId { get; set; } = string.Empty;
	public string ClientSecret { get; set; } = string.Empty;
	public string CalendarApiUrl { get; set; } = string.Empty;
	public string TokenApiUrl { get; set; } = string.Empty;
	public string AuthUrl { get; set; } = string.Empty;
	public string CalendarScope { get; set; } = string.Empty;
	public string ScopeFields { get; set; } = string.Empty;
	public string PlacesApiUrl { get; set; } = string.Empty;
	public string PlacesApiKey { get; set; } = string.Empty;
	public bool UseMockPlacesService { get; set; } = false;

	public string GetAuthUrl(string state, string redirectUri)
	{
		// First step: only request profile and email
		// Note: access_type=offline requests a refresh token
		// prompt=consent forces showing consent screen to ensure refresh token is returned
		string url = BuildAuthUrl(redirectUri, state, ScopeFields);
		return url + "&prompt=consent&access_type=offline";
	}

	public string GetCalendarAuthUrl(string state, string redirectUri, string email)
	{
		// Second step: request calendar scope incrementally with user's email to avoid account selection
		// Note: access_type=offline requests a refresh token
		// prompt=consent forces showing consent screen to ensure refresh token is returned
		string url = BuildAuthUrl(redirectUri, state, CalendarScope);
		url += "&include_granted_scopes=true&access_type=offline&prompt=consent";
		
		if (!string.IsNullOrEmpty(email))
			url += $"&login_hint={email.ToUrlEncoded()}";
		
		return url;
	}

	private string BuildAuthUrl(string redirectUri, string state, string scope)
	{
		return $"{AuthUrl}?client_id={ClientId.ToUrlEncoded()}&redirect_uri={redirectUri.ToUrlEncoded()}&response_type=code&scope={scope.ToUrlEncoded()}&state={state.ToUrlEncoded()}";
	}
}
