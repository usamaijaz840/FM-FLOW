using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FMFlow.Entities;

public class Integration
{
	[Key]
	public int IntegrationID { get; set; }

	[ForeignKey(nameof(User))]
	public int UserID { get; set; }
	public virtual FlowUser User { get; set; } = null!;
	public IntegrationType IntegrationType { get; set; }
	
	/// <summary>
	/// The access token for the integration
	/// </summary>
	public string Data { get; set; } = string.Empty;
	
	/// <summary>
	/// The refresh token for the integration (used to obtain new access tokens)
	/// </summary>
	public string? RefreshToken { get; set; }
	
	/// <summary>
	/// When the access token expires (UTC)
	/// </summary>
	public DateTimeOffset? TokenExpiresAt { get; set; }
}

public enum IntegrationType
{
	GoogleCalendar = 0,
	OutlookCalendar = 1
}