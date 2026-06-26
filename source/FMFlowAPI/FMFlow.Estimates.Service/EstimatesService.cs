using System.Data;
using Dapper;
using EFRepository;
using FluentValidation;
using FMFlow.AccessValidation;
using FMFlow.Common;
using FMFlow.Data;
using FMFlow.Email.Interface;
using FMFlow.Entities;
using FMFlow.Estimates.Interface;
using FMFlow.Estimates.Interface.DTOs;
using FMFlow.Estimates.Service.Mappers;
using FMFlow.FlowAPI.Interface;
using FMFlow.Identity.Interface;
using FMFlow.Leads.Interface;
using FMFlow.LeadTimelines.Interface;
using FMFlow.ProUser.Interface;
using FMFlow.Transactions.Interface.DTOs;
using FMFlow.Transactions.Interface.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FMFlow.Estimates.Service;

public class EstimatesService(
	IRepository repository,
	ApplicationDbContext dbContext,
	ICurrentUserService currentUserService,
	IAccessValidator accessValidator,
	IEstimateCalculatorService estimateCalculatorService,
	IProUserFileService proUserFilesService,
	ILeadTimelineService leadTimelineService,
	IEmailSenderService emailSenderService,
	IEstimateNotificationService estimateNotificationService,
	ILeadsService leadsService,
	IValidator<RequestedEstimateRequestDto> requestedEstimateValidator,
	IValidator<EstimateRequestDto> estimateValidator,
	IValidator<EstimateSendEmailsRequestDto> sendEmailValidator,
	ILogger<EstimatesService> logger) : IEstimatesService
{
	public async Task<Result<RequestedEstimateResponseDto>> CreateRequestedEstimate(RequestedEstimateRequestDto request, CancellationToken ct)
	{
		var requestValidation = await DtoValidator.Validate(request, requestedEstimateValidator, ct);

		if (!requestValidation.IsSuccess)
			return Result<RequestedEstimateResponseDto>.Failure(requestValidation.Error!);

		var accessResult = await accessValidator.ValidateAccessToProject(request.ProjectID, ct);

		if (!accessResult.IsSuccess)
			return Result<RequestedEstimateResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);

		var mapper = new EstimateMapper();
		var requestedEstimate = mapper.MapToRequestedEstimate(request);

		var project = await repository
			.Query<Project>()
			.Include(p => p.Lead)
				.ThenInclude(l => l.Customer)
			.ByProjectID(request.ProjectID)
			.FirstOrDefaultAsync(ct);

		if (project == null)
			return Result<RequestedEstimateResponseDto>.Failure("Project not found.", ResultErrorType.NotFound);

		if (currentUserService.IsPro())
		{
			requestedEstimate.ProUserID = currentUserService.GetUserID();
		}

		repository.AddNew(requestedEstimate);

		await repository.SaveAsync(ct);

		await emailSenderService.SendEmailCustomerRequestedEstimate(project.Lead, ct);

		var requestedEstimateResponse = mapper.MapToRequestedEstimateResponse(requestedEstimate);

		return Result<RequestedEstimateResponseDto>.Success(requestedEstimateResponse);
	}

	public async Task<Result<EstimateResponseDto>> CreateEstimate(EstimateRequestDto request, CancellationToken ct)
	{
		var requestValidation = await DtoValidator.Validate(request, estimateValidator, ct);

		if (!requestValidation.IsSuccess)
			return Result<EstimateResponseDto>.Failure(requestValidation.Error!);

		var requestedEstimate = await repository
			.Query<RequestedEstimate>()
			.Include(re => re.Project)
				.ThenInclude(p => p.Lead)
					.ThenInclude(l => l.Address)
			.Include(re => re.Project)
				.ThenInclude(p => p.Lead)
					.ThenInclude(l => l.Customer)
			.ByRequestedEstimateID(request.RequestedEstimateID)
			.FirstOrDefaultAsync(ct);

		if (requestedEstimate == null)
			return Result<EstimateResponseDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToRequestedEstimate(requestedEstimate!, ct);

		if (!accessResult.IsSuccess)
			return Result<EstimateResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);

		var mapper = new EstimateMapper();
		var estimate = mapper.MapToEstimate(request);

		estimate.StatusLastUpdate = DateTimeOffset.UtcNow;

		estimate.ProUser = await repository.Query<FlowUser>()
			.ByUserID(estimate.ProUserID)
			.Include(p => p.ProUser)
			.FirstAsync(ct);

		estimate.RequestedEstimate = requestedEstimate!;

		repository.AddNew(estimate);

		await repository.SaveAsync(ct);

		await leadTimelineService.RecordLeadTimelineAsync(estimate, TimelineEventKey.EstimateSent, ct);

		// Ensure the lead has a customer (FlowUser) record for payment processing
		var ensureCustomerResult = await leadsService.EnsureLeadHasCustomer(requestedEstimate.Project.Lead, ct);
		ensureCustomerResult.LogAnyErrors(logger);

		// Notify customer about reviewing the estimate
		string proBusinessName = estimate.ProUser.ProUser!.BusinessName ?? estimate.ProUser.GetFullName();

		var emailResult = await estimateNotificationService.SendReviewEstimateEmailToLead(
			requestedEstimate.Project.Lead, estimate.EstimateID, proBusinessName, ct);

		emailResult.LogAnyErrors(logger);

		string customerPhoneNumber = CustomerInfoHelper.GetCustomerPhoneNumber(estimate.RequestedEstimate.Project.Lead);

		estimateNotificationService.SendEstimateCreatedSmsToCustomer(
			estimate.EstimateID, proBusinessName, customerPhoneNumber);

		var estimateResponse = mapper.MapToEstimateResponse(estimate);

		return Result<EstimateResponseDto>.Success(estimateResponse);
	}



	public async Task<Result<DetailedEstimateResponseDto>> GetDetailedEstimate(int estimateId, CancellationToken ct)
	{
		var estimate = await repository
			.Query<Estimate>()
			.Include(e => e.RequestedEstimate)
				.ThenInclude(re => re.EstimateType)
			.Include(e => e.RequestedEstimate.Project)
				.ThenInclude(p => p.Lead)
					.ThenInclude(l => l.Address)
			.Include(e => e.RequestedEstimate.Project.Address)
				.ThenInclude(a => a.State)
			.Include(e => e.ProUser)
			.Include(e => e.ProUser.ProUser)
			.Include(e => e.ScheduledEstimate)
			.Include(e => e.EstimateRecipients)
			.ByEstimateID(estimateId)
			.FirstOrDefaultAsync(ct);

		if (estimate == null)
			return Result<DetailedEstimateResponseDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToEstimate(estimate, ct);

		if (!accessResult.IsSuccess)
			return Result<DetailedEstimateResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);

		var project = estimate.RequestedEstimate.Project;
		var lead = project.Lead;
		var address = project.Address;

		ct.ThrowIfCancellationRequested();

		var proUserFileRecord = await repository
			.Query<ProUserFile>()
			.ByUserID(estimate.ProUserID)
			.Include(x => x.File)
			.Where(x => x.ProFileType == ProUserFileType.Logo && !x.File.IsDeleted)
			.FirstOrDefaultAsync(ct);

		string? base64Image = null;

		if (proUserFileRecord != null)
		{
			ct.ThrowIfCancellationRequested();
			var proUserFile = await proUserFilesService.DownloadFile(estimate.ProUserID, proUserFileRecord.ProUserFileID, false, ct);
			base64Image = proUserFile.IsSuccess && proUserFile?.Value != null
				? $"data:{proUserFile.Value.ContentType};base64,{Convert.ToBase64String(proUserFile.Value.FileBytes)}"
				: null;
		}

		ct.ThrowIfCancellationRequested();

		// Load estimate recipients (just emails)
		var estimateRecipientEmails = estimate.EstimateRecipients
			.Where(er => !er.IsDeleted)
			.Select(er => er.RecipientEmail);

		// Create the detailed response
		var detailedResponse = new DetailedEstimateResponseDto(
			EstimateId: estimate.EstimateID,
			RequestedEstimateId: estimate.RequestedEstimateID,
			JobId: estimate.JobId,
			RequestedEstimateName: estimate.RequestedEstimate.Name,
			EstimateTypeName: estimate.RequestedEstimate.EstimateType.EstimateTypeName,
			ProUserId: estimate.ProUser != null ? estimate.ProUserID : null,
			Status: estimate.Status,
			StatusLastUpdate: estimate.StatusLastUpdate,
			IsOnHold: estimate.IsOnHold,
			IsActive: estimate.IsActive,
			Attributes: estimate.Attributes?.RootElement,
			CalculationResults: estimate.CalculationResults?.RootElement,
			Total: estimate.Total,
			HasBeenPaid: estimate.HasBeenPaid,
			PaidAmount: estimate.PaidAmount,
			ProjectId: project.ProjectID,
			ProjectSummary: project.Summary,
			AddressLine1: address.Line1,
			AddressLine2: address.Line2,
			City: address.City,
			State: address.State.Abbreviation,
			ZipCode: address.ZipCode,
			LeadId: lead.LeadID,
			FirstName: lead.FirstName,
			LastName: lead.LastName,
			Email: lead.Email,
			Mobile: lead.Mobile,
			PhoneNumber: lead.PhoneNumber,
			OrganizationName: lead.OrganizationName,
			IsChangeOrder: estimate.RequestedEstimate.IsChangeOrder,
			AssignedProName: estimate.ProUser != null ? $"{estimate.ProUser.GetFullName()}" : null,
			AssignedProEmail: estimate.ProUser?.Email,
			AssignedDateTime: estimate.ProUser != null ? estimate.RequestedEstimate.DateCreated : null,
			BusinessName: estimate.ProUser?.ProUser?.BusinessName,
			ScheduledEstimateId: estimate.ScheduledEstimateID,
			ScheduledDateTime: estimate.ScheduledEstimate?.ScheduledDateTime,
			ProUserLogoFileId: proUserFileRecord != null ? proUserFileRecord.FileID : null,
			AdditionalEmails: estimateRecipientEmails
		);

		return Result<DetailedEstimateResponseDto>.Success(detailedResponse);
	}

	public async Task<Result<RequestedEstimateResponseDto>> GetRequestedEstimate(int requestedEstimateId, CancellationToken ct)
	{
		var requestedEstimate = await repository
			.Query<RequestedEstimate>()
			.Include(re => re.Estimates)
			.Include(re => re.Project)
			.ThenInclude(p => p.Lead)
			.ByRequestedEstimateID(requestedEstimateId)
			.FirstOrDefaultAsync(ct);

		if (requestedEstimate == null)
			return Result<RequestedEstimateResponseDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToRequestedEstimate(requestedEstimate, ct);

		if (!accessResult.IsSuccess)
			return Result<RequestedEstimateResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);

		var currentUser = await currentUserService.GetCurrentUser(ct);

		if (currentUser == null)
			return Result<RequestedEstimateResponseDto>.Failure("Current user not found.", ResultErrorType.NotFound);

		if (currentUserService.IsPro())
		{
			var proZipCode = await repository
				.Query<ProUserDetail>()
				.ByUserID(currentUser.UserID)
				.Include(p => p.BusinessAddress)
				.Select(p => p.BusinessAddress.ZipCode)
				.FirstOrDefaultAsync(ct);

			if (proZipCode != requestedEstimate.Project.Lead.Address.ZipCode)
				return Result<RequestedEstimateResponseDto>.Failure("You are not authorized to see this requested estimate.");
		}

		if (currentUserService.IsAccountManager())
		{
			var assignedZipCodes = currentUser?.EmployeeUser?.AssignedZipCodes.Select(z => z.Zipcode).ToList();

			if (assignedZipCodes == null ||
				requestedEstimate.Project == null ||
				!assignedZipCodes.Contains(requestedEstimate.Project.Lead.Address.ZipCode))
				return Result<RequestedEstimateResponseDto>.Failure("You are not authorized to see this requested estimate.");
		}

		var mapper = new EstimateMapper();
		var requestedEstimateResponse = mapper.MapToRequestedEstimateResponse(requestedEstimate);

		return Result<RequestedEstimateResponseDto>.Success(requestedEstimateResponse);
	}

	public async Task<Result<RequestedEstimateResponseDto>> UpdateRequestedEstimate(int requestedEstimateId, RequestedEstimateUpdateRequestDto updateRequest, CancellationToken ct)
	{
		if (string.IsNullOrEmpty(updateRequest.Name))
			return Result<RequestedEstimateResponseDto>.Failure("Name is required.");

		var requestedEstimate = await repository
			.Query<RequestedEstimate>()
			.Include(re => re.Project)
			.ThenInclude(p => p.Lead)
			.ThenInclude(l => l.Address)
			.ByRequestedEstimateID(requestedEstimateId)
			.FirstOrDefaultAsync(ct);

		if (requestedEstimate == null)
			return Result<RequestedEstimateResponseDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToRequestedEstimate(requestedEstimate, ct);

		if (!accessResult.IsSuccess)
			return Result<RequestedEstimateResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);

		var currentUser = await currentUserService.GetCurrentUser(ct);
		if (currentUser == null)
			return Result<RequestedEstimateResponseDto>.Failure("Current user not found.", ResultErrorType.NotFound);

		var assignedZipCodes = currentUser.EmployeeUser?.AssignedZipCodes.Select(z => z.Zipcode).ToList();

		if (requestedEstimate.IsDeleted)
			return Result<RequestedEstimateResponseDto>.Failure(ErrorMessages.ResourceDeleted);

		if (currentUserService.IsPro())
		{
			if (requestedEstimate.ProUserID != currentUser.UserID)
			{
				return Result<RequestedEstimateResponseDto>.Failure("You are not authorized to modify this requested estimate.");
			}
		}
		else
		{
			if (assignedZipCodes == null ||
			requestedEstimate.Project == null ||
			!assignedZipCodes.Contains(requestedEstimate.Project.Lead.Address.ZipCode))
			{
				return Result<RequestedEstimateResponseDto>.Failure("You are not authorized to modify this requested estimate.");
			}
		}

		requestedEstimate.DateUpdated = DateTime.UtcNow;
		requestedEstimate.Name = updateRequest.Name;

		repository.AddOrUpdate(requestedEstimate);

		await repository.SaveAsync(ct);

		var mapper = new EstimateMapper();
		var requestedEstimateResponse = mapper.MapToRequestedEstimateResponse(requestedEstimate);

		return Result<RequestedEstimateResponseDto>.Success(requestedEstimateResponse);
	}

	public async Task<Result<DetailedEstimateResponseDto>> UpdateEstimate(int estimateId, EstimateUpdateRequestDto updateRequest, CancellationToken ct)
	{
		if (updateRequest == null || (updateRequest.Status == null && updateRequest.IsActive == null && updateRequest.IsOnHold == null && updateRequest.Attributes == null))
			return Result<DetailedEstimateResponseDto>.Failure("Invalid request.");

		var estimate = await repository
			.Query<Estimate>()
			.Include(e => e.ProUser)
			.Include(e => e.RequestedEstimate)
			.Include(e => e.RequestedEstimate.Project)
				.ThenInclude(p => p.Lead)
					.ThenInclude(l => l.Address)
			.Include(e => e.RequestedEstimate.Project)
				.ThenInclude(p => p.Lead)
					.ThenInclude(l => l.Customer)
			.ByEstimateID(estimateId)
			.FirstOrDefaultAsync(ct);

		if (estimate == null)
			return Result<DetailedEstimateResponseDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToEstimate(estimate, ct);

		if (!accessResult.IsSuccess)
			return Result<DetailedEstimateResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);

		var result = Result.Success();

		if (currentUserService.IsAccountManager())
		{
			result = await UpdateEstimateByAccountManager(estimate, updateRequest, ct);

			if (!result.IsSuccess)
				return Result<DetailedEstimateResponseDto>.Failure(result.Error!, result.ErrorType);

			repository.AddOrUpdate(estimate);
			await repository.SaveAsync(ct);
		}
		else if (currentUserService.IsCustomer() || currentUserService.IsEstimateRecipient())
		{
			result = await UpdateEstimateByCustomer(estimate, updateRequest, ct);

			if (!result.IsSuccess)
				return Result<DetailedEstimateResponseDto>.Failure(result.Error!, result.ErrorType);
		}
		else
		{
			if (updateRequest.Status == EstimateStatus.Approved &&
				estimate.Status != EstimateStatus.Approved)
				return Result<DetailedEstimateResponseDto>.Failure("Only Customers can approve estimates.");

			result = await ApplyStandardUpdateEstimateRules(estimate, updateRequest, ct);

			if (!result.IsSuccess)
				return Result<DetailedEstimateResponseDto>.Failure(result.Error!, result.ErrorType);

			repository.AddOrUpdate(estimate);

			await repository.SaveAsync(ct);
		}

		return await GetDetailedEstimate(estimateId, ct);
	}

	private async Task<Result> ApplyStandardUpdateEstimateRules(Estimate estimate, EstimateUpdateRequestDto updateRequest, CancellationToken ct)
	{
		if (updateRequest.Status != null && updateRequest.Status.Value != estimate.Status)
		{
			if (updateRequest.Status.Value == EstimateStatus.InProgress &&
				(!estimate.IsActive || estimate.Status != EstimateStatus.Scheduled))
			{
				return Result.Failure("It can be set to InProgress only when the status is Scheduled and the estimate is active.", ResultErrorType.BadRequest);
			}
			else if (updateRequest.Status.Value == EstimateStatus.Canceled &&
				(!estimate.IsActive || estimate.Status == EstimateStatus.Canceled))
			{
				return Result.Failure("It is either already cancelled or not active.", ResultErrorType.BadRequest);
			}

			estimate.Status = updateRequest.Status.Value;
			estimate.StatusLastUpdate = DateTimeOffset.UtcNow;

			if (estimate.Status == EstimateStatus.Scheduled)
				await leadTimelineService.RecordLeadTimelineAsync(estimate, TimelineEventKey.EstimateScheduled, ct, updateRequest.StatusUpdateReason);
			else if (estimate.Status == EstimateStatus.InProgress)
			{
				await leadTimelineService.RecordLeadTimelineAsync(estimate, TimelineEventKey.EstimateInProgress, ct, updateRequest.StatusUpdateReason);
				await emailSenderService.SendEmailCustomerEstimatorArrived(estimate, ct);
				// FUTURE: SMS service could be added here if needed for the status change notification
			}
		}

		if (updateRequest.IsActive.HasValue && updateRequest.IsActive.Value != estimate.IsActive)
		{
			if (updateRequest.IsActive.Value && estimate.IsActive)
				return Result.Failure("It has already been activated.", ResultErrorType.BadRequest);
			else if (!updateRequest.IsActive.Value && !estimate.IsActive)
				return Result.Failure("It has already been deactivated.", ResultErrorType.BadRequest);

			estimate.IsActive = updateRequest.IsActive.Value;
		}

		if (updateRequest.IsOnHold.HasValue && updateRequest.IsOnHold.Value != estimate.IsOnHold)
		{
			if (estimate.Status is EstimateStatus.Completed or EstimateStatus.Canceled or EstimateStatus.Scheduled)
				return Result.Failure("You can't set hold when the status is Completed, Canceled, or Scheduled.", ResultErrorType.BadRequest);

			if (updateRequest.IsOnHold.Value && estimate.IsOnHold)
				return Result.Failure("The estimate is already on hold.", ResultErrorType.BadRequest);

			if (!updateRequest.IsOnHold.Value && !estimate.IsOnHold)
				return Result.Failure("The estimate is not currently on hold.", ResultErrorType.BadRequest);

			if (!updateRequest.IsOnHold.Value && !estimate.IsActive)
				return Result.Failure("It has been deactivated.", ResultErrorType.BadRequest);

			estimate.IsOnHold = updateRequest.IsOnHold.Value;

			if (estimate.IsOnHold)
				await leadTimelineService.RecordLeadTimelineAsync(estimate, TimelineEventKey.EstimateOnHold, ct, updateRequest.StatusUpdateReason);
		}

		if (updateRequest.Attributes != null)
			estimate.Attributes = updateRequest.Attributes;

		if (updateRequest.IsOnHold.HasValue)
			estimate.IsOnHold = updateRequest.IsOnHold.Value;

		if (updateRequest.IsActive.HasValue)
			estimate.IsActive = updateRequest.IsActive.Value;

		if ((estimate.Status == EstimateStatus.InProgress || estimate.Status == EstimateStatus.Completed) && updateRequest.Attributes != null)
		{
			try
			{
				var calculationResults = estimateCalculatorService.Calculate(updateRequest.Attributes, estimate.ProUserID);
				estimate.CalculationResults = calculationResults.CalculationResultsJson;
				estimate.Total = calculationResults.Total;
			}
			catch
			{
				return Result.Failure("Invalid attributes for price calculation.");
			}
		}

		return Result.Success();
	}

	private async Task<Result> UpdateEstimateByAccountManager(Estimate estimate, EstimateUpdateRequestDto updateRequest, CancellationToken ct)
	{
		// AccountManager can only update Status (to Canceled), StatusUpdateReason, and IsActive
		
		// Check if trying to update disallowed fields
		if (updateRequest.IsOnHold.HasValue)
			return Result.Failure("AccountManagers do not have permission to update the IsOnHold field.", ResultErrorType.PermissionDenied);

		if (updateRequest.Attributes != null)
			return Result.Failure("AccountManagers do not have permission to update the Attributes field.", ResultErrorType.PermissionDenied);

		// Check if Status is provided and validate it can only be Canceled
		if (updateRequest.Status.HasValue)
		{
			if (updateRequest.Status.Value != EstimateStatus.Canceled)
				return Result.Failure("AccountManagers only have permission to change status to 'Canceled'.", ResultErrorType.PermissionDenied);

			if (estimate.Status == EstimateStatus.Canceled)
				return Result.Failure("The estimate is already cancelled.", ResultErrorType.BadRequest);

			if (!estimate.IsActive)
				return Result.Failure("The estimate must be active to be cancelled.", ResultErrorType.BadRequest);

			estimate.Status = EstimateStatus.Canceled;
			estimate.StatusLastUpdate = DateTimeOffset.UtcNow;
			await leadTimelineService.RecordLeadTimelineAsync(estimate, TimelineEventKey.EstimateCanceled, ct, updateRequest.StatusUpdateReason);
		}

		// Update IsActive if provided
		if (updateRequest.IsActive.HasValue && updateRequest.IsActive.Value != estimate.IsActive)
		{
			if (updateRequest.IsActive.Value && estimate.IsActive)
				return Result.Failure("It has already been activated.", ResultErrorType.BadRequest);
			else if (!updateRequest.IsActive.Value && !estimate.IsActive)
				return Result.Failure("It has already been deactivated.", ResultErrorType.BadRequest);

			estimate.IsActive = updateRequest.IsActive.Value;
		}

		// StatusUpdateReason is allowed but doesn't require a separate update since it's used with status changes above

		return Result.Success();
	}

	private async Task<Result<EstimateResponseDto>> UpdateEstimateByCustomer(Estimate estimate, EstimateUpdateRequestDto updateRequest, CancellationToken ct)
	{
		// Customer can only update Status (to Approved)
		
		// Check if trying to update disallowed fields
		if (updateRequest.IsOnHold.HasValue)
			return Result<EstimateResponseDto>.Failure("Customers do not have permission to the IsOnHold field.", ResultErrorType.PermissionDenied);

		if (updateRequest.IsActive.HasValue)
			return Result<EstimateResponseDto>.Failure("Customers do not have permission to the IsActive field.", ResultErrorType.PermissionDenied);

		if (updateRequest.Attributes != null)
			return Result<EstimateResponseDto>.Failure("Customers do not have permission to the Attributes field.", ResultErrorType.PermissionDenied);

		// Validate Status can only be set to Approved
		if (updateRequest.Status != EstimateStatus.Approved)
			return Result<EstimateResponseDto>.Failure("Customers only have permission to change status to 'Approved'.", ResultErrorType.PermissionDenied);

		if (estimate.Status != EstimateStatus.Completed)
			return Result<EstimateResponseDto>.Failure("Only completed estimates can be approved by a customer.", ResultErrorType.PermissionDenied);

		// Update current estimate to Approved
		estimate.Status = EstimateStatus.Approved;
		estimate.StatusLastUpdate = DateTimeOffset.UtcNow;
		repository.AddOrUpdate(estimate);

		// Cancel other estimates from the same project assigned to different Pros
		var estimatesToCancel = estimate.RequestedEstimate.Project.RequestedEstimates
			.SelectMany(re => re.Estimates)
			.Where(e =>
				e.EstimateID != estimate.EstimateID &&
				e.Status != EstimateStatus.Canceled &&
				e.ProUserID != estimate.ProUserID);

		foreach (var other in estimatesToCancel)
		{
			other.Status = EstimateStatus.Canceled;
			other.StatusLastUpdate = DateTimeOffset.UtcNow;
			repository.AddOrUpdate(other);
			await leadTimelineService.RecordLeadTimelineAsync(other, TimelineEventKey.EstimateCanceled, ct);
		}

		await leadTimelineService.RecordLeadTimelineAsync(estimate, TimelineEventKey.EstimateApproved, ct, updateRequest.StatusUpdateReason);

		await repository.SaveAsync(ct);

		decimal total = 0;

		if (estimate.Total.HasValue)
			total = estimate.Total.Value;

		var transaction = PaymentMapper.MapCustomerApproveToTransaction(new PaymentRequestDto(total, "", PaymentMethod.Manual), estimate.EstimateID);
		repository.AddNew(transaction);
		await repository.SaveAsync(ct);

		var mapper = new EstimateMapper();
		var response = mapper.MapToEstimateResponse(estimate);

		return Result<EstimateResponseDto>.Success(response);
	}

	public async Task<Result> DeleteRequestedEstimate(int requestedEstimateID, CancellationToken ct)
	{
		var requestedEstimate = await repository
			.Query<RequestedEstimate>()
			.Include(re => re.Estimates)
			.Include(re => re.Project)
			.ThenInclude(p => p.Lead)
			.ByRequestedEstimateID(requestedEstimateID)
			.FirstOrDefaultAsync(ct);

		if (requestedEstimate == null)
			return Result.Failure("Requested estimate not found.", ResultErrorType.NotFound);

		if (currentUserService.IsPro() && requestedEstimate.ProUserID != currentUserService.GetUserID())
			return Result.Failure("You are not authorized to delete this requested estimate.");

		if (currentUserService.IsAccountManager())
		{
			var currentUser = await currentUserService.GetCurrentUser(ct);

			if (currentUser == null)
				return Result.Failure("Current user not found.", ResultErrorType.NotFound);

			var assignedZipCodes = currentUser.EmployeeUser?.AssignedZipCodes.Select(z => z.Zipcode).ToList();

			if (assignedZipCodes == null ||
				requestedEstimate.Project == null ||
				!assignedZipCodes.Contains(requestedEstimate.Project.Lead.Address.ZipCode))
				return Result.Failure("You are not authorized to delete this requested estimate.");
		}

		if (requestedEstimate.IsDeleted)
			return Result.Failure("Can't delete deleted requested estimate.");

		if (requestedEstimate.Estimates.Any(e => e.Status != EstimateStatus.Scheduled))
			return Result.Failure("Can't delete requested estimate with estimates in any state other than 'scheduled'.");

		requestedEstimate.IsDeleted = true;
		requestedEstimate.DateDeleted = DateTime.UtcNow;

		foreach (var estimate in requestedEstimate.Estimates)
		{
			estimate.Status = EstimateStatus.Canceled;
			await leadTimelineService.RecordLeadTimelineAsync(estimate, TimelineEventKey.EstimateCanceled, ct);
		}

		repository.AddOrUpdate(requestedEstimate);

		await repository.SaveAsync(ct);

		return Result.Success();
	}

	public async Task<Result<SearchResult<SearchEstimatesResponseDto>>> SearchEstimates(int projectId, string? keywordSearch, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		var foundProject = await repository.Query<Project>()
			.Include(x => x.Lead.Address)
			.ByProjectID(projectId)
			.FirstOrDefaultAsync(ct);

		if (foundProject == null)
			return Result<SearchResult<SearchEstimatesResponseDto>>.Failure("Project not found.", ResultErrorType.NotFound);

		var query = repository.Query<Estimate>()
				.Include(e => e.ProUser)
				.Include(e => e.RequestedEstimate)
				.Where(e => e.RequestedEstimate.IsDeleted == false &&
				e.RequestedEstimate.ProjectID == projectId);

		if (!string.IsNullOrEmpty(keywordSearch))
		{
			if (keywordSearch.Length < 3 || keywordSearch.Length > 100)
				return Result<SearchResult<SearchEstimatesResponseDto>>.Failure("The keyword should be between 3 to 100 characters.");

			query = query
				.Where(p =>
				EF.Functions.ILike(p.RequestedEstimate.Name, $"%{keywordSearch}%"));
		}

		var currentUserID = currentUserService.GetUserID();

		if (currentUserService.IsPro())
		{
			query = query.Where(x => x.ProUserID == currentUserID);
		}
		else if (currentUserService.IsAccountManager())
		{
			var zipCodesForCurrentAccountManager = await repository
				.Query<ZipCode>()
				.Where(z => z.EmployeesAssigned.Any(ea => ea.UserID == currentUserID))
				.Select(p => p.Zipcode)
				.ToListAsync(ct);

			query = query.Where(l => zipCodesForCurrentAccountManager.Contains(foundProject.Lead.Address.ZipCode));
		}

		ct.ThrowIfCancellationRequested();

		var totalResults = await query.CountAsync(ct);

		var estimates = await query
			.ToListAsync(ct);

		var mapper = new EstimateMapper();

		var estimateDtos = new List<SearchEstimatesResponseDto>();

		foreach (var estimate in estimates)
		{
			var dto = mapper.MapToSearchEstimatesResponse(estimate);

			dto.AssignedProName = estimate.ProUser != null ? $"{estimate.ProUser.GetFullName()}" : null;
			dto.AssignedProEmail = estimate.ProUser?.Email;

			estimateDtos.Add(dto);
		}

		var searchResult = new SearchResult<SearchEstimatesResponseDto>(estimateDtos, totalResults);

		return Result<SearchResult<SearchEstimatesResponseDto>>.Success(searchResult);
	}

	public async Task<Result<List<RequestedEstimateResponseDto>>> GetRequestedEstimatesByProjectID(int projectId, CancellationToken ct)
	{
		var accessResult = await accessValidator.ValidateAccessToProject(projectId, ct);

		if (!accessResult.IsSuccess)
			return Result<List<RequestedEstimateResponseDto>>.Failure(accessResult.Error!, accessResult.ErrorType);

		var requestedEstimates = await repository
			.Query<RequestedEstimate>()
			.Include(re => re.Estimates)
			.Include(re => re.Project)
			.ThenInclude(p => p.Lead)
			.Where(re => re.ProjectID == projectId && !re.IsDeleted)
			.ToListAsync(ct);

		if (!requestedEstimates.Any())
			return Result<List<RequestedEstimateResponseDto>>.Success([]);

		var mapper = new EstimateMapper();
		var requestedEstimateResponses = requestedEstimates
			.Select(mapper.MapToRequestedEstimateResponse)
			.ToList();

		return Result<List<RequestedEstimateResponseDto>>.Success(requestedEstimateResponses);
	}

	public async Task<Result<Dictionary<string, SearchResult<KanbanEstimateOrJobResponseDto>>>> SearchEstimatesForKanban(
		KanbanEstimateStatus? estimateStatus,
		int? proUserId,
		int pageIndex,
		int pageSize,
		CancellationToken ct)
	{
		if (pageIndex < 0 || pageSize < 1)
			return Result<Dictionary<string, SearchResult<KanbanEstimateOrJobResponseDto>>>.Failure("Invalid pagination parameters.");

		pageSize = Math.Min(pageSize, 50);

		var minRowNum = pageIndex * pageSize + 1;
		var maxRowNum = minRowNum + pageSize;

		var filters = new List<string>
		{
			"""re."IsDeleted" = FALSE""",
			"""p."IsDeleted" = FALSE""",
			"""e."IsActive" = TRUE""",
			"""e."Status" != 'Canceled'""",
			"""e."StatusLastUpdate" > date_subtract(NOW(), '30 days')""",
			"""NOT EXISTS (SELECT 1 FROM "Jobs" j WHERE j."EstimateId" = e."EstimateID")"""
		};

		var parameters = new DynamicParameters();

		parameters.Add("minRowNum", minRowNum);
		parameters.Add("maxRowNum", maxRowNum);

		if (estimateStatus is not null)
		{
			filters.Add("""(CASE e."IsOnHold" WHEN TRUE THEN 'OnHold' ELSE e."Status" END) = :status""");
			parameters.Add("status", estimateStatus.ToString());
		}

		if (currentUserService.IsPro())
		{
			filters.Add("""e."ProUserID" = :proViewerUserId""");
			parameters.Add("proViewerUserId", currentUserService.GetUserID());
		}

		if (currentUserService.IsAccountManager())
		{
			if (proUserId.HasValue)
			{
				var accountManagerUserId = currentUserService.GetUserID();
				ct.ThrowIfCancellationRequested();
				var amZipCodes = repository
					.Query<ZipCode>()
					.Where(z => z.EmployeesAssigned.Any(ea => ea.UserID == accountManagerUserId))
					.Select(z => z.Zipcode);

				ct.ThrowIfCancellationRequested();
				var isProAssignedToAm = await repository
					.Query<ProUserToProZipcode>()
					.ByUserID(proUserId.Value)
					.ByIsDeleted(false)
					.Where(pz => amZipCodes.Contains(pz.Zipcode))
					.AnyAsync(ct);

				if (!isProAssignedToAm)
				{
					return Result<Dictionary<string, SearchResult<KanbanEstimateOrJobResponseDto>>>.Success(new Dictionary<string, SearchResult<KanbanEstimateOrJobResponseDto>>());
				}

				filters.Add("""e."ProUserID" = :proUserId""");
				parameters.Add("proUserId", proUserId.Value);
			}
			else
			{
				return Result<Dictionary<string, SearchResult<KanbanEstimateOrJobResponseDto>>>.Failure("proUserId is required for Account Managers.", ResultErrorType.BadRequest);
			}
		}

		var whereClause = string.Join(" AND ", filters);

		var sql = $"""
			WITH ranked_estimates AS
			(
			  SELECT
			     e."EstimateID" AS "Id"
			    ,(CASE e."IsOnHold" WHEN TRUE THEN 'OnHold' ELSE e."Status" END) AS "Status"  -- It may be possible to make this a computed column later
			    ,re."Name"
			    ,CONCAT(
			        a."Line1" || ', '
			       ,a."Line2" || ', '
			       ,a."City" || ', '
			       ,a."StateId"
			       ,a."ZipCode") AS "ProjectAddress"
			    ,CONCAT(fu."FirstName" || ' ', fu."LastName") AS "ProFullName"
			    ,CONCAT(l."FirstName" || ' ', l."LastName") AS "CustomerName"
			    ,e."Total" AS "Amount"
				,e."DepositHasBeenPaid" AS "DepositHasBeenPaid"
			    ,et."EstimateTypeName"
			    ,ROW_NUMBER() OVER (PARTITION BY (CASE e."IsOnHold" WHEN TRUE THEN 'OnHold' ELSE e."Status" END) ORDER BY re."DateCreated" DESC) AS rn
				,COUNT(*) OVER (PARTITION BY (CASE e."IsOnHold" WHEN TRUE THEN 'OnHold' ELSE e."Status" END)) AS "StatusTotal"
			  FROM
			    "Estimates" e
			    JOIN "RequestedEstimates" re ON e."RequestedEstimateID" = re."RequestedEstimateID"
			    JOIN "Projects" p ON re."ProjectID" = p."ProjectID"
			    JOIN "Addresses" a ON p."AddressID" = a."AddressID"
			    JOIN "EstimateTypes" et ON re."EstimateTypeId" = et."EstimateTypeId"
			    LEFT JOIN "FMFlowUsers" fu ON e."ProUserID" = fu."UserID"
			    LEFT JOIN "Leads" l ON p."LeadID" = l."LeadID"
			  WHERE
			    {whereClause}
			)
			SELECT
			   res."Id"
			  ,res."Status"
			  ,res."Name"
			  ,res."ProjectAddress"
			  ,res."ProFullName"
			  ,res."CustomerName"
			  ,res."Amount"
			  ,res."DepositHasBeenPaid"
			  ,res."EstimateTypeName"
			  ,res."StatusTotal"
			FROM
			  ranked_estimates res
			WHERE
			  res.rn >= :minRowNum
			  AND res.rn < :maxRowNum;
			""";

		ct.ThrowIfCancellationRequested();

		using var connection = new NpgsqlConnection(dbContext.Database.GetConnectionString());
		var queryResults = await connection.QueryAsync<ExtendedKanbanItem>(sql, parameters);

		var results = queryResults.GroupBy(result => result.Status)
			.ToDictionary(g => g.Key, g => new SearchResult<KanbanEstimateOrJobResponseDto>(g.Select(r => new KanbanEstimateOrJobResponseDto(
				r.Id,
				r.Status.ToCamelCase(),
				r.Name,
				r.ProjectAddress,
				r.ProFullName,
				r.Amount,
				r.DepositHasBeenPaid,
				r.EstimateTypeName,
				r.CustomerName
			)), (int)(g.FirstOrDefault()?.StatusTotal ?? 0)));

		ct.ThrowIfCancellationRequested();

		return Result<Dictionary<string, SearchResult<KanbanEstimateOrJobResponseDto>>>.Success(results);
	}

	public async Task<Result> SendAdditionalEstimateFinalizedEmails(int estimateId, EstimateSendEmailsRequestDto request, CancellationToken ct)
	{
		var requestValidation = await DtoValidator.Validate(request, sendEmailValidator, ct);

		if (!requestValidation.IsSuccess)
			return Result<EstimateResponseDto>.Failure(requestValidation.Error!);

		var estimate = await repository
			.Query<Estimate>()
			.Include(e => e.EstimateRecipients)
			.Include(e => e.RequestedEstimate)
				.ThenInclude(re => re.Project)
					.ThenInclude(p => p.Lead)
						.ThenInclude(l => l.Customer)
			.Include(e => e.RequestedEstimate)
				.ThenInclude(re => re.ProUser)
					.ThenInclude(pro => pro!.ProUser)
			.ByEstimateID(estimateId)
			.FirstOrDefaultAsync(ct);

		if (estimate == null)
			return Result.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToEstimate(estimate, ct);

		if (!accessResult.IsSuccess)
			return Result<EstimateResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);

		return await estimateNotificationService.SendAdditionalEstimateFinalizedEmails(estimate, request, ct);
	}

	public async Task<Result> ResendEstimateReviewEmail(ResendEstimateReviewEmailRequestDto request, CancellationToken ct)
	{
		return await estimateNotificationService.ResendEstimateReviewEmail(request, ct);
	}

}

record ExtendedKanbanItem(
	int Id,
	string Status,
	string Name,
	string ProjectAddress,
	string ProFullName,
	string? CustomerName,
	decimal? Amount,
	bool? DepositHasBeenPaid,
	string EstimateTypeName,
	long StatusTotal) :
	KanbanEstimateOrJobResponseDto(Id, Status, Name, ProjectAddress, ProFullName, Amount, DepositHasBeenPaid, EstimateTypeName, CustomerName);
