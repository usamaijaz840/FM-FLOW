namespace FMFlow.Data.Identity;

public class KeycloakConfiguration
{
	public string KeycloakBaseUrl { get; set; } = string.Empty;
	public string ClientId { get; set; } = string.Empty;
	public string ClientSecret { get; set; } = string.Empty;
	public string KeycloakRealm { get; set; } = string.Empty;
}
