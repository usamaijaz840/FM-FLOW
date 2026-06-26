namespace FMFlow.Email.Service;

public class EmailTemplateIdsSettings
{
	public const string SectionName = "EmailTemplatesIds";
	public string ProAdvancedPayment { get; set; } = string.Empty;
	public string ProCustomerApproved { get; set; } = string.Empty;
	public string ProEstimateFullyPaid { get; set; } = string.Empty;
	public string ProOnboardingComplete { get; set; } = string.Empty;
	public string CustomerResidentialEstimateScheduled { get; set; } = string.Empty;
	public string CustomerEstimatorArrived { get; set; } = string.Empty;
	public string CustomerJobScheduled { get; set; } = string.Empty;
	public string CustomerResidentialJobScheduled { get; set; } = string.Empty;
	public string CustomerSignOffSuccessful { get; set; } = string.Empty;
	public string CustomerJobCancelled { get; set; } = string.Empty;
	public string CustomerPaymentSuccessful { get; set; } = string.Empty;
	public string CustomerRequestedEstimate { get; set; } = string.Empty;
	public string CustomerReviewEstimate { get; set; } = string.Empty;
	public string ResetPasswordEmail { get; set; } = string.Empty;
}
