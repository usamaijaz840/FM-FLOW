using System.Diagnostics.CodeAnalysis;

namespace FMFlow.Email.Interface.Templates;

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "snake_case expected by SendGrid")]
public record CustomerEstimatorArrived(
	string recipient_name,
	string estimate_title,
	string estimator_name,
	string hero_image_url,
	string referral_logo_url,
	string logo_url,
	string year,
	string facebook_url,
	string instagram_url,
	string linkedin_url);

