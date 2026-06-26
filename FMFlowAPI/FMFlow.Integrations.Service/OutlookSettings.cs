using FMFlow.Common.Extensions;

namespace FMFlow.Integrations.Service;

public class OutlookSettings
{
	public const string SectionName = "Outlook";
	public string Tenant { get; set; } = string.Empty;
	public string ClientId { get; set; } = string.Empty;
	public string ClientSecret { get; set; } = string.Empty;
	public string MSAuthUrl { get; set; } = string.Empty;
	public string TokenApiUri { get; set; } = string.Empty;
	public string OAuthUri { get; set; } = string.Empty;
	public string ScopeFields { get; set; } = string.Empty;

	public string GetAuthUrl(string state, string redirectUri)
	{
		// Include offline_access scope to request a refresh token
		return $"{MSAuthUrl}/{Tenant}{OAuthUri}?client_id={ClientId.ToUrlEncoded()}&redirect_uri={redirectUri.ToUrlEncoded()}&response_type=code&scope={ScopeFields.ToUrlEncoded()}&state={state.ToUrlEncoded()}&prompt=select_account";
	}

	public string GetTokenUrl()
	{
		return $"{MSAuthUrl}/{Tenant}{TokenApiUri}";
	}
}
