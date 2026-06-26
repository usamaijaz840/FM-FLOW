namespace FMFlow.Identity;

/// <summary>
/// Custom claim type constants for FMFlow authentication
/// </summary>
public static class CustomClaimTypes
{
	/// <summary>
	/// External ID claim from Keycloak - contains FlowUser.UserID
	/// This gets transformed to SubjectId claim via FMFlowClaimsTransformation
	/// </summary>
	public const string ExternalId = "external_id";

	/// <summary>
	/// Preferred username claim - contains the user's email address
	/// </summary>
	public const string PreferredUsername = "preferred_username";

	/// <summary>
	/// Lead ID claim - contains the Lead.LeadID for lead-related tokens
	/// </summary>
	public const string LeadId = "lead_id";

	/// <summary>
	/// Estimate ID claim - contains the Estimate.EstimateID for estimate-related tokens
	/// </summary>
	public const string EstimateId = "estimate_id";

	/// <summary>
	/// Token purpose claim - indicates the intended use of the token
	/// </summary>
	public const string TokenPurpose = "purpose";
}
