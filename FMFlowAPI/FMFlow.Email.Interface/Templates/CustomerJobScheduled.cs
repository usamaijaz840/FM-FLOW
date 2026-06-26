using System.Diagnostics.CodeAnalysis;

namespace FMFlow.Email.Interface.Templates;

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "snake_case expected by SendGrid")]
public record CustomerJobScheduled(
	string recipient_name,
	string company_name,
	string job_title,
	string pro_business_name,
	string pro_name,
	string scheduled_display,
	string phone,
	string email,
	string avatar_url,
	string referral_logo_url,
	string footer_logo_url,
	string year,
	string facebook_url,
	string instagram_url,
	string linkedin_url,
	string collections_image_url,
	string referral_source,
	Prep_Blocks[] prep_blocks);

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "snake_case expected by SendGrid")]
public record Prep_Blocks(
	string text,
	string image_url);
