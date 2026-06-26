using System.Diagnostics.CodeAnalysis;

namespace FMFlow.Email.Interface.Templates;

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "snake_case expected by SendGrid")]
public record PasswordResetEmail(
	string user_name,
	string app_name,
	string company_name,
	string year,
	string referral_logo_url,
	string footer_logo_url,
	string reset_link,
	string expires_minutes,
	string support_url,
	string support_email,
	string support_phone);