using System.Diagnostics.CodeAnalysis;

namespace FMFlow.Email.Interface.Templates;

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "snake_case expected by SendGrid")]
public record CustomerJobCancelled(
	string recipient_name,
	string project_title,
	string year,
	string referral_logo_url,
	string footer_logo_url,
	string facebook_url,
	string instagram_url,
	string linkedin_url,
	string unsubscribe,
	string unsubscribe_preferences,
	CustomerJobCancelledPro[] pros);

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "snake_case expected by SendGrid")]
public record CustomerJobCancelledPro(
	string pro_business_name,
	string pro_name,
	string phone,
	string email,
	string avatar_url);
