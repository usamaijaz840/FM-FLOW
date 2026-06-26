using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;

namespace FMFlow.Email.Interface;

public interface IEmailSenderService
{
	Task<Result> SendEmailProAdvancedPayment(Estimate estimate, decimal amount, CancellationToken ct);

	Task<Result> SendEmailProCustomerApproved(Estimate estimate, CancellationToken ct);

	Task<Result> SendEmailProEstimateFullyPaid(Estimate estimate, CancellationToken ct);

	Task<Result> SendEmailProOnboardingComplete(ProUserDetail pro, CancellationToken ct);

	Task<Result> SendEmailCustomerResidentialEstimateScheduled(List<int> scheduledEstimatesIds, CancellationToken ct);

	Task<Result> SendEmailCustomerEstimatorArrived(Estimate estimate, CancellationToken ct);

	Task<Result> SendEmailCustomerJobScheduled(Job job, CancellationToken ct);

	Task<Result> SendEmailCustomerResidentialJobScheduled(Job job, CancellationToken ct);

	Task<Result> SendEmailCustomerSignOffSuccessful(Estimate estimate, CancellationToken ct);

	Task<Result> SendEmailCustomerJobCancelled(Project project, CancellationToken ct);

	Task<Result> SendEmailCustomerPaymentSuccessful(Estimate estimate, decimal paymentAmount, CancellationToken ct);

	Task<Result> SendEmailCustomerRequestedEstimate(Lead lead, CancellationToken ct);

	Task<Result> SendEmailCustomerReviewEstimate(string customerFullName, string viewEstimateLink, string recipientEmail,
		string? businessName, CancellationToken ct);

	Task<Result> SendEmailPasswordResetLink(FlowUser user, string resetLink, CancellationToken ct);
}
