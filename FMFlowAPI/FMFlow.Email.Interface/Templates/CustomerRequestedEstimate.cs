using System.Diagnostics.CodeAnalysis;

namespace FMFlow.Email.Interface.Templates;

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "snake_case expected by SendGrid")]
public record CustomerRequestedEstimate(
	string recipient_name,
	string support_phone,
	string brand_name,
	string referral_logo_url,
	string footer_logo_url,
	string hero_image_url,
	string year,
	string facebook_url,
	string instagram_url,
	string linkedin_url,
	string unsubscribe,
	string unsubscribe_preferences);
