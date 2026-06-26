using FMFlow.Entities;
using FMFlow.Estimates.Interface.DTOs;
using FMFlow.FlowAPI.Interface;

namespace FMFlow.Estimates.Interface;

public interface IEstimateRecipientsService
{
	/// <summary>
	/// Retrieves the recipients associated with a specified estimate.
	/// </summary>
	/// <param name="estimateId">The unique identifier of the estimate.</param>
	/// <param name="ct">A cancellation token for the asynchronous operation.</param>
	/// <returns>
	/// A <see cref="Result{T}"/> containing a collection of <see cref="string"/> representing the email addresses of the recipients of the estimate.
	/// </returns>
	Task<Result<IEnumerable<string>>> GetEstimateRecipientEmails(int estimateId, CancellationToken ct);

	/// <summary>
	/// Sets the recipients for a specified estimate by updating existing recipients and adding new ones.
	/// Marks recipients as deleted if their email is not present in the provided list, and adds new recipients for emails not already associated with the estimate.
	/// </summary>
	/// <param name="estimateId">The unique identifier of the estimate.</param>
	/// <param name="recipientEmails">A list of recipient email addresses to be associated with the estimate.</param>
	/// <param name="ct">A cancellation token for the asynchronous operation.</param>
	/// <returns>
	/// A <see cref="Result{T}"/> containing a collection of <see cref="EstimateRecipientResponseDto"/> representing the recipients of the estimate.
	/// </returns>
	Task<Result> SetEstimateRecipients(Estimate estimate, List<string> emails, CancellationToken ct);
}
