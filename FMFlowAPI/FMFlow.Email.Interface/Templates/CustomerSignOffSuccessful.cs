using System.Diagnostics.CodeAnalysis;

namespace FMFlow.Email.Interface.Templates;

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "snake_case expected by SendGrid")]
public record CustomerSignOffSuccessful(
	string recipient_name,
	string project_title,
	string customer_name,
	string pro_name,
	string job_total,
	string already_paid,
	string amount_due,
	string final_payment_url,
	string logo_url,
	string referral_logo_url,
	string year,
	string facebook_url,
	string instagram_url,
	string linkedin_url,
	string referral_source,
	string unsubscribe);

