using System.Diagnostics.CodeAnalysis;

namespace FMFlow.Email.Interface.Templates;

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "snake_case expected by SendGrid")]
public record CustomerReviewEstimate(
	string recipient_name,
	string business_name,
	string estimation_url,
	string referral_logo_url,
	string footer_logo_url,
	string year,
	string facebook_url,
	string instagram_url,
	string linkedin_url,
	string unsubscribe,
	string unsubscribe_preferences);
