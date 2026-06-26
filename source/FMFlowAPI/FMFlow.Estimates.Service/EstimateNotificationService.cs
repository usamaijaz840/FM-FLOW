using EFRepository;
using FluentValidation;
using FMFlow.Common;
using FMFlow.Email.Interface;
using FMFlow.Entities;
using FMFlow.Estimates.Interface;
using FMFlow.Estimates.Interface.DTOs;
using FMFlow.FlowAPI.Interface;
using FMFlow.Leads.Interface;
using FMFlow.SMS.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FMFlow.Estimates.Service;

public class EstimateNotificationService(
	IRepository repository,
	IEmailSenderService emailSenderService,
	ISMSSenderService smsSenderService,
	IMagicLinkService magicLinkService,
	IEstimateRecipientsService estimateRecipientsService,
	ILeadsService leadsService,
	IValidator<ResendEstimateReviewEmailRequestDto> resendEstimateReviewEmailValidator,
	ILogger<EstimateNotificationService> logger) : IEstimateNotificationService
{
	public async Task<Result> SendReviewEstimateEmailToLead(
		Lead lead,
		int estimateId,
		string proBusinessName,
		CancellationToken ct)
	{
		if (!lead.CustomerID.HasValue)
			return Result.Failure("Lead does not have a customer. This should not happen after EnsureLeadHasCustomer.");

		string customerFullName = CustomerInfoHelper.GetCustomerFullName(lead);
		string customerEmail = CustomerInfoHelper.GetCustomerEmail(lead);

		if (string.IsNullOrEmpty(customerEmail))
			return Result.Failure("Lead does not have a valid email address.");

		var result = await magicLinkService.GenerateCustomerEstimateMagicLink(estimateId, lead.CustomerID.Value, ct)
			.MapResult(async (string link, CancellationToken ct2) =>
			{
				return await emailSenderService.SendEmailCustomerReviewEstimate(
					customerFullName,
					viewEstimateLink: link,
					recipientEmail: customerEmail,
					businessName: proBusinessName,
					ct2);
			}, ct);

		return result;
	}

	public void SendEstimateCreatedSmsToCustomer(int estimateId, string proBusinessName, string customerPhoneNumber)
	{
		smsSenderService.SendSmsCustomerProCreatedEstimate(estimateId, proBusinessName, customerPhoneNumber);
	}

	public async Task<Result> SendAdditionalEstimateFinalizedEmails(
		Estimate estimate,
		EstimateSendEmailsRequestDto request,
		CancellationToken ct)
	{
		// Ensure the lead has a customer (FlowUser) record for payment processing
		await leadsService.EnsureLeadHasCustomer(estimate.RequestedEstimate.Project.Lead, ct);

		string proBusinessName = estimate.ProUser.ProUser!.BusinessName ?? estimate.ProUser.GetFullName();

		if (request.SendToLead)
		{
			var emailResult = await SendReviewEstimateEmailToLead(
				estimate.RequestedEstimate.Project.Lead,
				estimate.EstimateID,
				proBusinessName,
				ct);
			emailResult.LogAnyErrors(logger);
		}

		var result = await estimateRecipientsService.SetEstimateRecipients(estimate, request.AdditionalEmailAddresses, ct);

		if (!result.IsSuccess)
		{
			result.LogAnyErrors(logger);
			return Result.Failure("Failed to set additional recipients.");
		}

		foreach (EstimateRecipient recipient in estimate.EstimateRecipients)
		{
			Result<string> generateLinkResult = await magicLinkService.GenerateEstimateRecipientMagicLink(
				estimate.EstimateID,
				recipient.EstimateRecipientId,
				ct);

			if (!generateLinkResult.IsSuccess)
			{
				generateLinkResult.LogAnyErrors(logger);
				continue;
			}

			var sendEmailResult = await emailSenderService.SendEmailCustomerReviewEstimate(
				string.Empty,
				generateLinkResult.Value!,
				recipientEmail: recipient.RecipientEmail,
				businessName: proBusinessName,
				ct);

			sendEmailResult.LogAnyErrors(logger);
		}

		return Result.Success();
	}

	public async Task<Result> ResendEstimateReviewEmail(ResendEstimateReviewEmailRequestDto request, CancellationToken ct)
	{
		var validationResult = await resendEstimateReviewEmailValidator.ValidateAsync(request, ct);

		if (!validationResult.IsValid)
		{
			logger.LogWarning("Invalid resend estimate review email request: {Errors}",
				string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));
			return Result.Failure("Invalid request.");
		}

		// Look up the old nonce (even if expired or consumed) to get entity info
		Nonce? oldNonce = await repository.Query<Nonce>()
			.Where(n => n.Value == request.Nonce)
			.OrderByDescending(n => n.CreatedAt)
			.FirstOrDefaultAsync(ct);

		if (oldNonce == null)
		{
			logger.LogWarning("Nonce not found for resend request. Nonce was deleted or never existed.");
			return Result.Failure("This link is no longer valid. Please request a new estimate or contact support.");
		}

		string? proBusinessName = await repository.Query<Estimate>()
			.Where(e => e.EstimateID == request.EstimateId)
			.Include(e => e.ProUser)
				.ThenInclude(pro => pro!.ProUser)
			.Select(e => e.ProUser!.ProUser!.BusinessName ?? e.ProUser!.GetFullName())
			.FirstOrDefaultAsync(ct);

		if (proBusinessName == null)
		{
			logger.LogWarning("Pro business name not found for estimate ID: {EstimateId}", request.EstimateId);
			return Result.Failure("Unable to retrieve pro business information.");
		}

		// Generate new nonce and send email based on nonce type
		Result result = oldNonce.Type switch
		{
			NonceType.CustomerMagicLink => await ResendReviewEstimateEmailToCustomer(
				oldNonce.EntityId,
				request.EstimateId,
				proBusinessName,
				ct),
			NonceType.EstimateRecipientMagicLink => await ResendReviewEstimateToEstimateRecipient(
				oldNonce.EntityId,
				request.EstimateId,
				proBusinessName,
				ct),
			_ => Result.Failure("Unsupported nonce type for resend.")
		};

		if (!result.IsSuccess)
		{
			logger.LogError("Failed to resend magic link for entity {EntityId}, type {Type}: {Error}",
				oldNonce.EntityId, oldNonce.Type, result.Error);

			// Return specific error messages for NotFound errors, otherwise use generic message
			if (result.ErrorType == ResultErrorType.NotFound)
				return result;

			return Result.Failure("Unable to send new link. Please try again or contact support.");
		}

		// Delete the old nonce now that we've successfully sent a new one
		// This prevents the same expired nonce from being used to request multiple resends
		repository.Delete(oldNonce);
		await repository.SaveAsync(ct);

		logger.LogInformation("Deleted nonce {Nonce} after successful resend for entity {EntityId}, type {Type}",
			request.Nonce, oldNonce.EntityId, oldNonce.Type);

		return Result.Success();
	}

	private async Task<Result> ResendReviewEstimateToEstimateRecipient(
		int estimateRecipientId,
		int estimateId,
		string proBusinessName,
		CancellationToken ct)
	{
		var estimateRecipient = await repository.Query<EstimateRecipient>()
			.Where(er => er.EstimateRecipientId == estimateRecipientId && !er.IsDeleted)
			.FirstOrDefaultAsync(ct);

		if (estimateRecipient == null)
			return Result.Failure("Estimate recipient not found.", ResultErrorType.NotFound);

		if (estimateRecipient.EstimateId != estimateId)
			return Result.Failure("Estimate recipient does not match the provided estimate ID.", ResultErrorType.NotFound);

		var estimateLinkResult = await magicLinkService.GenerateEstimateRecipientMagicLink(
			estimateRecipient.EstimateId,
			estimateRecipient.EstimateRecipientId,
			ct);

		if (!estimateLinkResult.IsSuccess)
			return Result.Failure(estimateLinkResult.Error ?? "Failed to generate magic link");

		var result = await emailSenderService.SendEmailCustomerReviewEstimate(
			string.Empty,
			estimateLinkResult.Value!,
			recipientEmail: estimateRecipient.RecipientEmail,
			proBusinessName,
			ct);

		return result;
	}

	private async Task<Result> ResendReviewEstimateEmailToCustomer(
		int customerId,
		int estimateId,
		string proBusinessName,
		CancellationToken ct)
	{
		// Retrieve the customer (FlowUser)
		var customer = await repository.Query<FlowUser>()
			.Where(u => u.UserID == customerId)
			.FirstOrDefaultAsync(ct);

		if (customer == null)
			return Result.Failure("Customer not found.", ResultErrorType.NotFound);

		// Verify the customer is authorized to access this estimate
		// Uses the same validation logic as AccessValidator for Customer role
		var estimate = await repository.Query<Estimate>()
			.Include(e => e.RequestedEstimate)
				.ThenInclude(re => re.Project)
					.ThenInclude(p => p.Lead)
			.Where(e => e.EstimateID == estimateId)
			.FirstOrDefaultAsync(ct);

		if (estimate == null || estimate.RequestedEstimate?.Project?.Lead?.CustomerID != customerId)
			return Result.Failure("Customer not authorized for this estimate.", ResultErrorType.NotFound);

		var result = await magicLinkService.GenerateCustomerEstimateMagicLink(estimateId, customerId, ct)
			.MapResult(async (string link, CancellationToken ct2) =>
			{
				return await emailSenderService.SendEmailCustomerReviewEstimate(
					customer.GetFullName(),
					viewEstimateLink: link,
					recipientEmail: customer.Email,
					businessName: proBusinessName,
					ct2);
			}, ct);

		return result;
	}
}
