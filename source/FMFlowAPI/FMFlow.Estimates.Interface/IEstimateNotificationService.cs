using FMFlow.Common;
using FMFlow.Entities;
using FMFlow.Estimates.Interface.DTOs;
using FMFlow.FlowAPI.Interface;

namespace FMFlow.Estimates.Interface;

/// <summary>
/// Service for managing estimate-related notifications (email and SMS).
/// </summary>
public interface IEstimateNotificationService
{
	/// <summary>
	/// Sends a review estimate email to the lead's email address.
	/// </summary>
	Task<Result> SendReviewEstimateEmailToLead(Lead lead, int estimateId, string proBusinessName, CancellationToken ct);

	/// <summary>
	/// Sends estimate review notification to the customer via SMS.
	/// </summary>
	void SendEstimateCreatedSmsToCustomer(int estimateId, string proBusinessName, string customerPhoneNumber);

	/// <summary>
	/// Sends additional estimate finalized emails to the lead and/or additional recipients.
	/// </summary>
	Task<Result> SendAdditionalEstimateFinalizedEmails(
		Estimate estimate,
		EstimateSendEmailsRequestDto request,
		CancellationToken ct);

	/// <summary>
	/// Resends an estimate review email based on an expired or consumed nonce.
	/// </summary>
	Task<Result> ResendEstimateReviewEmail(ResendEstimateReviewEmailRequestDto request, CancellationToken ct);
}
