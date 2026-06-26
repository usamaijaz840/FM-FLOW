using System.Diagnostics.CodeAnalysis;

namespace FMFlow.Email.Interface.Templates;

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "snake_case expected by SendGrid")]
public record CustomerPaymentSuccessful(
	string recipient_name,
	string customer_name,
	string final_payment_amount,
	string project_title,
	string total_job_cost,
	string total_paid,
	string estimator_company_name,
	string hero_image_url,
	string referral_logo_url,
	string footer_logo_url,
	string review_url,
	string year,
	string unsubscribe,
	string unsubscribe_preferences,
	string google_review_url,
	string yelp_review_url,
	string create_account_url,
	string login_url);

