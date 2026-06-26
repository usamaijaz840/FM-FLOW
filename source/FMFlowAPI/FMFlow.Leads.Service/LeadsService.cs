using EFRepository;
using FluentValidation;
using FMFlow.AccessValidation;
using FMFlow.Common;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Identity.Interface;
using FMFlow.Leads.Interface;
using FMFlow.Leads.Interface.DTOs;
using FMFlow.Leads.Service.Mappers;
using FMFlow.LeadTimelines.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FMFlow.Leads.Service;

public class LeadsService(
	IRepository repository,
	ICurrentUserService currentUserService,
	IAccessValidator accessValidator,
	ILeadTimelineService leadTimelineService,
	IValidator<LeadRequestDto> leadRequestValidator,
	IValidator<LeadUpdateRequestDto> leadUpdateRequestValidator,
	ILogger<LeadsService> logger)
	: ILeadsService
{
	public async Task<Result<LeadResponseDto>> CreateLead(LeadRequestDto request, CancellationToken ct)
	{
		var requestValidation = await DtoValidator.Validate(request, leadRequestValidator, ct);

		if (!requestValidation.IsSuccess)
			return Result<LeadResponseDto>.Failure(requestValidation.Error!);

		var currentUserId = currentUserService.GetUserID();

		if (request.LeadSourceId.HasValue)
		{
			if (currentUserService.IsPro())
				return Result<LeadResponseDto>.Failure("Pro users cannot set lead sources when creating leads.");

			var leadSource = await repository
				.Query<LeadSource>()
				.ByLeadSourceID(request.LeadSourceId)
				.FirstOrDefaultAsync(ct);

			if (leadSource == null)
				return Result<LeadResponseDto>.Failure("Lead source not found.", ResultErrorType.NotFound);
		}
		else if (currentUserService.IsAccountManager())
			return Result<LeadResponseDto>.Failure("Account managers must set lead sources when creating leads.");

		var state = await repository
			.Query<State>()
			.ByAbbreviation(request.State)
			.FirstOrDefaultAsync(ct);

		if (state == null)
			return Result<LeadResponseDto>.Failure("State not found.", ResultErrorType.NotFound);

		var zipCodeIsValid = await repository
				.Query<ZipCode>()
				.ByZipcode(request.ZipCode)
				.AnyAsync(ct);

		if (!zipCodeIsValid)
			return Result<LeadResponseDto>.Failure("Invalid zip code.");

		var mapper = new LeadMapper();
		var lead = mapper.MapToLead(request);

		lead.Address = LeadMapper.CreateAddressFromLeadRequest(request, state);

		if (currentUserService.IsPro())
			lead.ProUserID = currentUserId;
		else if (currentUserService.IsScheduler())
			lead.SchedulerId = currentUserId;

		repository.AddNew(lead);

		await repository.SaveAsync(ct);

		await leadTimelineService.RecordLeadTimelineAsync(lead, TimelineEventKey.LeadCreated, ct);

		var response = LeadMapper.MapToLeadResponseDto(lead, currentUserService.IsFMEmployee());
		return Result<LeadResponseDto>.Success(response);
	}

	public async Task<Result<SearchResult<LeadResponseDto>>> SearchLeads(
		string? keywordSearch,
		bool? uncategorizedLeads,
		bool? includeScheduleComplete,
		int pageIndex,
		int pageSize,
		CustomerType? customerType,
		bool returnAllLeads,
		CancellationToken ct)
	{
		if (pageIndex < 0 || pageSize < 1)
			return Result<SearchResult<LeadResponseDto>>.Failure("Invalid pagination parameters.");

		pageSize = Math.Min(pageSize, 50);

		var query = repository.Query<Lead>().ByIsDeleted(false);

		if (!string.IsNullOrEmpty(keywordSearch))
		{
			if (keywordSearch.Length < 2 || keywordSearch.Length > 100)
				return Result<SearchResult<LeadResponseDto>>.Failure(ErrorMessages.KeywordTooShort);

			query = query
				.Where(l =>
					EF.Functions.ILike(l.FirstName, $"%{keywordSearch}%") ||
					EF.Functions.ILike(l.LastName, $"%{keywordSearch}%") ||
					EF.Functions.ILike(l.Email, $"%{keywordSearch}%") ||
					EF.Functions.ILike(l.Mobile, $"%{keywordSearch}%") ||
					(l.OrganizationName != null && EF.Functions.ILike(l.OrganizationName, $"%{keywordSearch}%")));
		}

		if (uncategorizedLeads.HasValue && uncategorizedLeads.Value)
		{
			query = query.Where(l =>
				!repository.Query<ProUserToProZipcode>()
					.Select(pz => pz.Zipcode)
					.Contains(l.Address.ZipCode)
				&& l.SchedulerId == null // Filter leads without a SchedulerId
			);
		}

		if (currentUserService.IsScheduler() &&
			!includeScheduleComplete.GetValueOrDefault())
		{
			query = query.ByScheduleComplete(false);
		}

		var currentUserId = currentUserService.GetUserID();

		if (currentUserService.IsAccountManager())
		{
			ct.ThrowIfCancellationRequested();
			var zipCodesForCurrentAccountManager = await repository
				.Query<ZipCode>()
				.Where(z => z.EmployeesAssigned.Any(ea => ea.UserID == currentUserId))
				.Select(p => p.Zipcode)
				.ToListAsync(ct);

			query = query.Where(l =>
			zipCodesForCurrentAccountManager.Contains(l.Address.ZipCode) ||
			(l.Projects != null &&
			l.Projects.Any(p =>
				!p.IsDeleted &&
				p.Address != null &&
				zipCodesForCurrentAccountManager.Contains(p.Address.ZipCode))));
		}
		else if (currentUserService.IsPro())
		{
			query = query.ByProUserID(currentUserId);
		}

		if (customerType != null)
			query = query.Where(l => l.CustomerType == customerType);

		var dupes = await GetDuplicateLeads(query, ct);

		var totalResults = await query.CountAsync(ct);

		var currentUserIsFMEmployee = currentUserService.IsFMEmployee();

		var allLeads = await query
			.Include(l => l.Scheduler)
			.Include(l => l.Address)
			.ThenInclude(a => a.State)
			.Include(l => l.LeadSource)
			.Select(l => new { Lead = l, DTO = LeadMapper.MapToLeadResponseDto(l, currentUserIsFMEmployee) })
			.ToListAsync(ct);

		foreach (var l in allLeads)
		{
			var leadId = l.DTO.LeadID;

			if (dupes.TryGetValue(leadId, out var connected))
			{
				l.DTO.DuplicateLeadIDs = connected;
			}
		}

		var sorted = allLeads
			.OrderByDescending(x => x.Lead.DateCreated)
			.Select(x => x.DTO)
			.ToList();

		var paged = sorted
			.Skip(pageIndex * pageSize)
			.Take(pageSize)
			.ToList();

		foreach (var dto in paged)
		{
			if (dupes.TryGetValue(dto.LeadID, out var connected))
				dto.DuplicateLeadIDs = connected;
		}

		var searchResult = new SearchResult<LeadResponseDto>(paged, totalResults);

		return Result<SearchResult<LeadResponseDto>>.Success(searchResult);
	}

	private static async Task<Dictionary<int, List<int>>> GetDuplicateLeads(IQueryable<Lead> query, CancellationToken ct)
	{
		var filteredLeads = await query.Select(x => new
		{
			x.LeadID,
			x.Email,
			x.PhoneNumber,
			x.Mobile
		}).ToListAsync(ct);

		var dupes = new Dictionary<int, List<int>>();

		var emailGroups = filteredLeads
			.Where(l => !string.IsNullOrEmpty(l.Email))
			.GroupBy(l => l.Email)
			.Where(g => g.Count() > 1);

		foreach (var group in emailGroups)
		{
			var ids = group.Select(x => x.LeadID).ToList();

			foreach (var id in ids)
			{
				if (!dupes.ContainsKey(id))
					dupes[id] = [];

				dupes[id].AddRange(ids.Where(x => x != id));
			}
		}

		var phoneGroups = filteredLeads
			.SelectMany(l => new[]
			{
				new { Phone = l.PhoneNumber, l.LeadID },
				new { Phone = l.Mobile, l.LeadID }
			})
			.Where(x => !string.IsNullOrEmpty(x.Phone))
			.GroupBy(x => x.Phone)
			.Where(g => g.Count() > 1);

		foreach (var group in phoneGroups)
		{
			var ids = group.Select(x => x.LeadID).ToList();

			foreach (var id in ids)
			{
				if (!dupes.ContainsKey(id))
					dupes[id] = [];

				dupes[id].AddRange(ids.Where(x => x != id));
			}
		}

		foreach (var key in dupes.Keys.ToList())
		{
			dupes[key] = [.. dupes[key].Distinct()];
		}

		return dupes;
	}

	public async Task<Result<LeadResponseDto>> UpdateLead(int leadId, LeadUpdateRequestDto leadUpdateRequest, CancellationToken ct)
	{
		var requestValidation = await DtoValidator.Validate(leadUpdateRequest, leadUpdateRequestValidator, ct);

		if (!requestValidation.IsSuccess)
		{
			return Result<LeadResponseDto>.Failure(requestValidation.Error!);
		}

		var query = repository.Query<Lead>().ByLeadID(leadId);

		query = query.Include(l => l.Address);
		query = query.Include(l => l.LeadSource);
		query = query.Include(l => l.Scheduler);

		if (currentUserService.IsPro())
		{
			query = query.Include(l => l.Projects);
		}

		var lead = await query.FirstOrDefaultAsync(ct);

		if (lead == null)
		{
			return Result<LeadResponseDto>.Failure("Lead not found.", ResultErrorType.NotFound);
		}

		var accessResult = await accessValidator.ValidateAccessToLead(lead, ct);

		if (!accessResult.IsSuccess)
		{
			return Result<LeadResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);
		}

		if (lead.IsDeleted)
		{
			return Result<LeadResponseDto>.Failure("Can't update deleted lead.");
		}

		var state = await repository
			.Query<State>()
			.ByAbbreviation(leadUpdateRequest.State)
			.FirstOrDefaultAsync(ct);

		if (state == null)
		{
			return Result<LeadResponseDto>.Failure("State not found.", ResultErrorType.NotFound);
		}

		var mapper = new LeadMapper();
		mapper.UpdateLead(leadUpdateRequest, lead);

		if (lead.Address == null)
		{
			lead.Address = LeadMapper.CreateAddressFromLeadUpdateRequest(leadUpdateRequest, state);
		}
		else
		{
			LeadMapper.UpdateAddressFromLeadUpdateRequest(leadUpdateRequest, lead.Address, state);
		}

		lead.DateUpdated = DateTimeOffset.UtcNow;

		var updateSchedulerResult = await SetScheduler(leadUpdateRequest, lead, ct);

		if (!updateSchedulerResult.IsSuccess)
		{
			return Result<LeadResponseDto>.Failure(updateSchedulerResult.Error!);
		}

		if (currentUserService.IsScheduler() && leadUpdateRequest.ScheduleComplete == true)
		{
			if (!lead.CanSetScheduleComplete)
			{
				return Result<LeadResponseDto>.Failure("Can't set ScheduleComplete for Leads without ScheduledEstimates.");
			}

			lead.ScheduleComplete = true;
		}

		repository.AddOrUpdate(lead);

		await repository.SaveAsync(ct);

		var response = LeadMapper.MapToLeadResponseDto(lead, currentUserService.IsFMEmployee());
		return Result<LeadResponseDto>.Success(response);
	}

	private async Task<Result> SetScheduler(LeadUpdateRequestDto leadUpdateRequest, Lead lead, CancellationToken ct)
	{
		if (currentUserService.IsScheduler())
		{
			if (leadUpdateRequest.SchedulerId == currentUserService.GetUserID())
				lead.Scheduler = await repository.Query<FlowUser>()
					.ByUserID(leadUpdateRequest.SchedulerId)
					.FirstAsync(ct);
			else
				return Result.Failure("Schedulers can only assign leads to themselves.", ResultErrorType.PermissionDenied);
		}
		else if (currentUserService.IsSuperAdmin())
		{
			if (leadUpdateRequest.SchedulerId == null)
			{
				lead.SchedulerId = null; // Clear scheduler if not set
				return Result.Success();
			}

			var scheduler = await repository.Query<FlowUser>()
				.ByUserID(leadUpdateRequest.SchedulerId)
				.Where(fu => fu.EmployeeUser != null && fu.EmployeeUser.Role == Roles.Scheduler.ToString())
				.FirstOrDefaultAsync(ct);

			if (scheduler != null)
				lead.Scheduler = scheduler;
			else
				return Result.Failure("Scheduler not found.", ResultErrorType.NotFound);
		}

		return Result.Success();
	}

	public async Task<Result> DeleteLead(int leadId, CancellationToken ct)
	{
		var query = repository.Query<Lead>().ByLeadID(leadId);

		if (currentUserService.IsPro())
		{
			query = query.Include(l => l.Projects);
		}

		var lead = await query.FirstOrDefaultAsync(ct);

		if (lead == null)
			return Result.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToLead(lead, ct);

		if (!accessResult.IsSuccess)
			return Result<LeadResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);

		if (lead.IsDeleted)
			return Result.Failure("Can't delete deleted lead.");

		lead.IsDeleted = true;
		lead.DateDeleted = DateTimeOffset.UtcNow;

		repository.AddOrUpdate(lead);

		await repository.SaveAsync(ct);

		return Result.Success();
	}

	public async Task<Result<LeadResponseDto>> GetLeadById(int leadId, CancellationToken ct)
	{
		var lead = await repository
			.Query<Lead>()
			.ByLeadID(leadId)
			.Include(l => l.Address)
			.ThenInclude(a => a.State)
			.Include(l => l.LeadSource)
			.FirstOrDefaultAsync(ct);

		if (lead == null)
		{
			return Result<LeadResponseDto>.Failure("Lead not found.", ResultErrorType.NotFound);
		}

		if (lead.IsDeleted)
		{
			return Result<LeadResponseDto>.Failure("Lead has been deleted.");
		}

		var accessResult = await accessValidator.ValidateAccessToLead(lead, ct);

		if (!accessResult.IsSuccess)
		{
			return Result<LeadResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);
		}

		var response = LeadMapper.MapToLeadResponseDto(lead, currentUserService.IsFMEmployee());

		return Result<LeadResponseDto>.Success(response);
	}

	public async Task<Result> EnsureLeadHasCustomer(Lead lead, CancellationToken ct)
	{
		logger.LogInformation("EnsureLeadHasCustomer called for lead {LeadID} with email {Email}", lead.LeadID, lead.Email);

		// If lead already has a customer, nothing to do
		if (lead.CustomerID.HasValue)
		{
			logger.LogInformation("Lead {LeadID} already has customer {CustomerID}", lead.LeadID, lead.CustomerID);
			return Result.Success();
		}

		// Search for existing user by email (including deactivated users)
		var existingUser = await repository
			.Query<FlowUser>()
			.Where(u => u.Email.ToLower() == lead.Email.ToLower())
			.FirstOrDefaultAsync(ct);

		if (existingUser != null && existingUser.UserID.HasValue)
		{
			// Link existing customer to lead
			logger.LogInformation("Linking existing customer {CustomerID} to lead {LeadID} with email {Email}", existingUser.UserID, lead.LeadID, lead.Email);
			lead.CustomerID = existingUser.UserID;
			repository.AddOrUpdate(lead);
			await repository.SaveAsync(ct);

			return Result.Success();
		}
		else
		{
			// Create new customer user in FMFlowUser database only (not in Keycloak)
			logger.LogInformation("No existing customer found for email {Email}. Creating new customer user for lead {LeadID}", lead.Email, lead.LeadID);
			var newCustomerUser = new FlowUser
			{
				Email = lead.Email,
				FirstName = lead.FirstName,
				LastName = lead.LastName,
				PhoneNumber = lead.PhoneNumber ?? lead.Mobile,
				DateCreated = DateTimeOffset.UtcNow
			};

			repository.AddNew(newCustomerUser);
			await repository.SaveAsync(ct);

			// Ensure the new customer user has a generated UserID
			if (!newCustomerUser.UserID.HasValue)
			{
				logger.LogError("Failed to generate UserID for new customer with email {Email} for lead {LeadID}", lead.Email, lead.LeadID);
				return Result.Failure("Failed to generate UserID for new customer.");
			}

			// Link the new customer to the lead
			lead.CustomerID = newCustomerUser.UserID;
			logger.LogInformation("Created new customer user {CustomerID} for lead {LeadID} with email {Email}", newCustomerUser.UserID, lead.LeadID, lead.Email);
			repository.AddOrUpdate(lead);
			await repository.SaveAsync(ct);

			return Result.Success();
		}
	}
}
