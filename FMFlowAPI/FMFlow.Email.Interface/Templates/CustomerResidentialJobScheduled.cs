using System.Diagnostics.CodeAnalysis;

namespace FMFlow.Email.Interface.Templates;

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "snake_case expected by SendGrid")]
public record CustomerResidentialJobScheduled(
	string recipient_name,
	string pro_business_name,
	string pro_name,
	string pro_phone,
	string pro_email,
	string scheduled_display,
	string referral_logo_url,
	string footer_logo_url,
	string pro_avatar_url,
	string collections_image_url,
	Prep_Blocks[] prep_blocks,
	string referral_source,
	string facebook_url,
	string instagram_url,
	string linkedin_url,
	string year,
	string unsubscribe);
