using System.Diagnostics.CodeAnalysis;

namespace FMFlow.Email.Interface.Templates;

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "snake_case expected by SendGrid")]
public record ProOnboardingComplete(
	string recipient_name,
	string login_url,
	string logo_url,
	string referral_logo_url,
	string facebook_url,
	string instagram_url,
	string linkedin_url,
	string year);

