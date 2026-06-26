using EFRepository;
using FMFlow.AccessValidation;
using FMFlow.Entities;
using FMFlow.Estimates.Interface;
using FMFlow.FlowAPI.Interface;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.Estimates.Service;

public class EstimateRecipientsService(
	IRepository _repository,
	IAccessValidator _accessValidator) : IEstimateRecipientsService
{
	/// <inheritdoc />
	public async Task<Result<IEnumerable<string>>> GetEstimateRecipientEmails(int estimateId, CancellationToken ct)
	{
		Estimate? estimate = await _repository
			.Query<Estimate>()
			.ByEstimateID(estimateId)
			.Include(e => e.EstimateRecipients)
			.FirstOrDefaultAsync(ct);

		if (estimate == null)
			return Result<IEnumerable<string>>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await _accessValidator.ValidateAccessToEstimate(estimate, ct)
			.MapResult(() =>
			{
				IEnumerable<string> recipientEmails = estimate.EstimateRecipients
					.Where(r => !r.IsDeleted)
					.Select(r => r.RecipientEmail);

				return Result<IEnumerable<string>>.Success(recipientEmails);

			}, ct);

		return accessResult;

	}

	/// <inheritdoc />
	public async Task<Result> SetEstimateRecipients(Estimate estimate, List<string> recipientEmails, CancellationToken ct)
	{
		if (estimate == null)
			return Result.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await _accessValidator.ValidateAccessToEstimate(estimate, ct);

		if (!accessResult.IsSuccess)
			return Result.Failure(accessResult.Error!, accessResult.ErrorType);

		var now = DateTimeOffset.UtcNow;

		// Build lookup of requested emails (value tracks if email already exists in database)
		var recipientEmailExistsInDatabase = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
		foreach (string email in recipientEmails)
		{
			recipientEmailExistsInDatabase[email] = false; // assume not existing yet
		}

		// Loop through existing recipients to toggle deletion status as needed
		foreach (EstimateRecipient recipient in estimate.EstimateRecipients)
		{
			bool shouldKeep = recipientEmailExistsInDatabase.ContainsKey(recipient.RecipientEmail);

			// If we are keeping this email, add an entry to the lookup to indicate it already exists in the database 
			if (shouldKeep)
			{
				recipientEmailExistsInDatabase[recipient.RecipientEmail] = true;
			}

			// Toggle deletion status and update timestamps if needed
			if (recipient.IsDeleted == shouldKeep)
			{
				recipient.IsDeleted = !shouldKeep;
				recipient.DateUpdated = now;
				recipient.DateDeleted = recipient.IsDeleted ? now : null;
			}
		}

		// Add new recipients (emails where alreadyExists is still false)
		foreach (var (email, existsInDatabase) in recipientEmailExistsInDatabase)
		{
			if (!existsInDatabase)
			{
				estimate.EstimateRecipients.Add(new EstimateRecipient
				{
					EstimateId = estimate.EstimateID,
					RecipientEmail = email,
					DateCreated = now,
					DateUpdated = now,
					IsDeleted = false
				});
			}
		}

		await _repository.SaveAsync(ct);

		return Result.Success();
	}
}
