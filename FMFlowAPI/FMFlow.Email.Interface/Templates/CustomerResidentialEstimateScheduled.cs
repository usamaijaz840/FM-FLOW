using System.Diagnostics.CodeAnalysis;

namespace FMFlow.Email.Interface.Templates;

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "snake_case expected by SendGrid")]
public record CustomerResidentialEstimateScheduled(
	string recipient_name,
	string login_url,
	string create_account_url,
	string logo_url,
	string referral_logo_url,
	string year,
	string facebook_url,
	string instagram_url,
	string linkedin_url,
	string referral_source,
	string chips_image_url,
	string collections_image_url,
	EstimateTemplate[] estimates);

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "snake_case expected by SendGrid")]
public record EstimateTemplate(
	string avatar_url,
	string pro_name,
	string pro_business_name,
	string scheduled_display,
	string phone,
	string email);
