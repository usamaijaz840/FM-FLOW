using EFRepository;
using FluentValidation;
using FMFlow.AccessValidation;
using FMFlow.Common;
using FMFlow.Email.Interface;
using FMFlow.Entities;
using FMFlow.Estimates.Interface;
using FMFlow.Estimates.Interface.DTOs;
using FMFlow.FlowAPI.Interface;
using FMFlow.Identity.Interface;
using FMFlow.Projects.Interface;
using FMFlow.Projects.Interface.DTOs;
using FMFlow.Projects.Service.Mapper;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.Projects.Service;

public class ProjectsService(
	IRepository repository,
	ICurrentUserService currentUserService,
	IAccessValidator accessValidator,
	IScheduledEstimatesService scheduledEstimatesService,
	IValidator<ProjectRequestDto> requestValidator,
	IValidator<CustomerProjectRequestDto> customerRequestValidator,
	IValidator<ProjectUpdateRequestDto> updateRequestValidator,
	IEmailSenderService emailSenderService) : IProjectsService
{
	public async Task<Result<ProjectResponseDto>> CreateProject(ProjectRequestDto request, CancellationToken ct)
	{
		var requestValidation = await DtoValidator.Validate(request, requestValidator, ct);

		if (!requestValidation.IsSuccess)
			return Result<ProjectResponseDto>.Failure(requestValidation.Error!);

		if (request.RequestedEstimates == null || !request.RequestedEstimates.Any(s => !string.IsNullOrWhiteSpace(s.Name)))
			return Result<ProjectResponseDto>.Failure("At least one requested estimate is required.");

		if (request.RequestedEstimates.Any(re => re.EstimateTypeId <= 0))
			return Result<ProjectResponseDto>.Failure("Estimate type is required for each requested estimate.");

		ct.ThrowIfCancellationRequested();

		var state = await repository
			.Query<State>()
			.ByAbbreviation(request.State)
			.FirstOrDefaultAsync(ct);

		if (state == null)
			return Result<ProjectResponseDto>.Failure("State not found.", ResultErrorType.NotFound);

		ct.ThrowIfCancellationRequested();

		var zipCodeIsValid = await repository
			.Query<ZipCode>()
			.ByZipcode(request.ZipCode)
			.AnyAsync(ct);

		if (!zipCodeIsValid)
			return Result<ProjectResponseDto>.Failure("Invalid zip code.");

		var leadID = request.LeadID;
		ct.ThrowIfCancellationRequested();

		var lead = await repository
			.Query<Lead>()
			.Include(l => l.Projects)
			.Include(l => l.Address)
			.ByLeadID(leadID)
			.FirstOrDefaultAsync(ct);

		if (lead == null)
			return Result<ProjectResponseDto>.Failure("Lead not found.", ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToLead(lead, ct);

		if (!accessResult.IsSuccess)
			return Result<ProjectResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);

		var address = new Address
		{
			Line1 = request.AddressLine1,
			Line2 = request.AddressLine2,
			State = state,
			City = request.City,
			ZipCode = request.ZipCode,
		};

		var mapper = new ProjectMapper();

		var project = mapper.MapToProject(request);
		project.LeadID = leadID;
		project.Address = address;
		project.Lead = lead;

		project.RequestedEstimates = [.. request.RequestedEstimates
			.Select(requestEstimate => new RequestedEstimate
			{
				Name = requestEstimate.Name,
				ProUserID = currentUserService.IsPro() ? currentUserService.GetUserID() : null,
				IsDeleted = false,
				EstimateTypeId = requestEstimate.EstimateTypeId,
			})];

		if (currentUserService.IsPro())
		{
			project.ProId = currentUserService.GetUserID();
		}

		repository.AddNew(project);
		ct.ThrowIfCancellationRequested();
		await repository.SaveAsync(ct);

		var createdScheduledEstimatesIds = new List<int>();

		if (currentUserService.IsPro() && request.ScheduleDateTime.HasValue)
		{
			var response = await scheduledEstimatesService.CreateScheduledEstimate(
				new ScheduledEstimateRequestDto
				(
					project.ProjectID,
					currentUserService.GetUserID(),
					request.ScheduleDateTime.Value,
					null,
					null
				),
				ct);

			if (response.Value != null)
			{
				createdScheduledEstimatesIds.Add(response.Value.ScheduledEstimateId);
			}
		}

		await emailSenderService.SendEmailCustomerResidentialEstimateScheduled(createdScheduledEstimatesIds, ct);

		var projectResponse = ProjectMapper.MapToProjectResponseDto(project);

		return Result<ProjectResponseDto>.Success(projectResponse);
	}

	public async Task<Result<ProjectResponseDto>> CreateCustomerProject(CustomerProjectRequestDto request, CancellationToken ct)
	{
		var requestValidation = await DtoValidator.Validate(request, customerRequestValidator, ct);

		if (!requestValidation.IsSuccess)
			return Result<ProjectResponseDto>.Failure(requestValidation.Error!);

		ct.ThrowIfCancellationRequested();

		var state = await repository
			.Query<State>()
			.ByAbbreviation(request.State)
			.FirstOrDefaultAsync(ct);

		if (state == null)
			return Result<ProjectResponseDto>.Failure("State not found.", ResultErrorType.NotFound);

		ct.ThrowIfCancellationRequested();

		var zipCodeIsValid = await repository
			.Query<ZipCode>()
			.ByZipcode(request.ZipCode)
			.AnyAsync(ct);

		if (!zipCodeIsValid)
			return Result<ProjectResponseDto>.Failure("Invalid zip code.");

		var leadID = request.LeadID;
		ct.ThrowIfCancellationRequested();

		var lead = await repository
			.Query<Lead>()
			.Include(l => l.Projects)
			.Include(l => l.Address)
			.ByLeadID(leadID)
			.FirstOrDefaultAsync(ct);

		if (lead == null)
			return Result<ProjectResponseDto>.Failure("Lead not found.", ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToLead(lead, ct);

		if (!accessResult.IsSuccess)
			return Result<ProjectResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);

		var address = new Address
		{
			Line1 = request.AddressLine1,
			Line2 = request.AddressLine2,
			State = state,
			City = request.City,
			ZipCode = request.ZipCode,
		};

		var mapper = new ProjectMapper();

		var project = mapper.MapToCustomerProject(request);
		project.LeadID = leadID;
		project.Address = address;
		project.Lead = lead;

		// Customer projects don't have requested estimates initially
		project.RequestedEstimates = [];

		// Customer is the lead owner, not a Pro
		project.ProId = null;

		repository.AddNew(project);
		ct.ThrowIfCancellationRequested();
		await repository.SaveAsync(ct);

		var projectResponse = ProjectMapper.MapToProjectResponseDto(project);

		return Result<ProjectResponseDto>.Success(projectResponse);
	}

	public async Task<Result<SearchResult<ProjectResponseDto>>> SearchProjects(
		int? leadId,
		string? keywordSearch,
		bool? isOpen,
		bool? isDeleted,
		DateTimeOffset? lastUpdated,
		int? proUserId,
		string? zipCode,
		int pageIndex,
		int pageSize,
		CancellationToken ct)
	{
		if (pageIndex < 0 || pageSize < 1)
			return Result<SearchResult<ProjectResponseDto>>.Failure("Invalid pagination parameters.");

		pageSize = Math.Min(pageSize, 50);

		ct.ThrowIfCancellationRequested();

		// Default to active-only results. Customers and schedulers should never be able to fetch deleted/archived projects.
		var isDeletedFilter = isDeleted ?? false;
		if (currentUserService.IsCustomer() || currentUserService.IsScheduler() || currentUserService.IsTempCustomer())
		{
			isDeletedFilter = false;
		}

		var query = repository.Query<Project>()
			.ByIsDeleted(isDeletedFilter);

		if (!string.IsNullOrEmpty(keywordSearch))
		{
			if (keywordSearch.Length < 2 || keywordSearch.Length > 100)
				return Result<SearchResult<ProjectResponseDto>>.Failure(ErrorMessages.KeywordTooShort);

			query = query
				.Where(p =>
					EF.Functions.ILike(p.Lead.FirstName, $"%{keywordSearch}%") ||
					EF.Functions.ILike(p.Lead.LastName, $"%{keywordSearch}%") ||
					EF.Functions.ILike(p.Title, $"%{keywordSearch}%"));
		}

		if (leadId.HasValue)
		{
			var leadExists = await repository
				.Query<Lead>()
				.ByLeadID(leadId.Value)
				.AnyAsync(ct);

			if (!leadExists)
				return Result<SearchResult<ProjectResponseDto>>.Failure("Lead not found.", ResultErrorType.NotFound);

			query = query.ByLeadID(leadId.Value);
		}

		if (isOpen.HasValue)
		{
			query = query.ByIsOpen(isOpen.Value);
		}

		if (!string.IsNullOrEmpty(zipCode))
		{
			query = query.Where(p => EF.Functions.ILike(p.Address.ZipCode, $"%{zipCode}%"));
		}

		if (lastUpdated.HasValue)
		{
			// Convert query param to UTC since Npgsql only supports comparing DateTimeOffset as UTC
			var utcLastUpdated = lastUpdated.Value.ToUniversalTime();

			query = query.Where(p =>
			p.DateUpdated >= utcLastUpdated ||
			(p.DateUpdated == null && p.DateCreated >= utcLastUpdated));
		}

		var currentUserId = currentUserService.GetUserID();

		if (currentUserService.IsCustomer())
		{
			query = query.Where(p => p.Lead.CustomerID == currentUserId);
		}

		// if current user is pro, ignore proId from the request and user currentUserId instead
		int? proIdFilter = currentUserService.IsPro() ? currentUserId : proUserId.HasValue ? proUserId.Value : null;

		if (proIdFilter.HasValue)
		{
			query = query.Where(p =>
				p.ProId == currentUserId ||
				p.Lead.ProUserID == proIdFilter.Value ||
				p.RequestedEstimates.Any(re => !re.IsDeleted && re.ProUserID == proIdFilter.Value) ||
				p.RequestedEstimates.Any(re => !re.IsDeleted && re.Estimates.Any(e => e.ProUserID == proIdFilter.Value)));
		}

		if (currentUserService.IsAccountManager())
		{
			var zipCodesForCurrentAccountManager = await repository
				.Query<ZipCode>()
				.Where(z => z.EmployeesAssigned.Any(ea => ea.UserID == currentUserId))
				.Select(p => p.Zipcode)
				.ToListAsync(ct);

			query = query.Where(p => zipCodesForCurrentAccountManager.Contains(p.Address.ZipCode));
		}

		ct.ThrowIfCancellationRequested();
		var totalResults = await query.CountAsync(ct);

		ct.ThrowIfCancellationRequested();

		var projects = await query
			.OrderByDescending(p => p.DateCreated)
			.Skip(pageIndex * pageSize)
			.Take(pageSize)
			.Include(p => p.Address)
			.ThenInclude(p => p.State)
			.Include(p => p.Lead)
			.ToListAsync(ct);

		var projectDtos = projects
			.Select(ProjectMapper.MapToProjectResponseDto)
			.ToList();

		var searchResult = new SearchResult<ProjectResponseDto>(projectDtos, totalResults);

		return Result<SearchResult<ProjectResponseDto>>.Success(searchResult);
	}

	public async Task<Result<ProjectResponseDto>> UpdateProject(int projectId, ProjectUpdateRequestDto updateRequest, CancellationToken ct)
	{
		var requestValidation = await DtoValidator.Validate(updateRequest, updateRequestValidator, ct);

		if (!requestValidation.IsSuccess)
			return Result<ProjectResponseDto>.Failure(requestValidation.Error!);

		ct.ThrowIfCancellationRequested();

		if (updateRequest.LeadID != null)
		{
			var leadID = updateRequest.LeadID;

			var leadExists = await repository
				.Query<Lead>()
				.ByLeadID(leadID)
				.ByIsDeleted(false)
				.AnyAsync(ct);

			if (!leadExists)
				return Result<ProjectResponseDto>.Failure("Lead not found.", ResultErrorType.NotFound);
		}

		ct.ThrowIfCancellationRequested();

		var project = await repository
			.Query<Project>()
			.Include(p => p.Address)
			.Include(p => p.Lead)
			.Include(p => p.RequestedEstimates)
				.ThenInclude(re => re.ProUser)
			.ByProjectID(projectId)
			.FirstOrDefaultAsync(ct);

		if (project == null)
			return Result<ProjectResponseDto>.Failure("Project not found.", ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToProject(project, ct);

		if (!accessResult.IsSuccess)
			return Result<ProjectResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);

		if (currentUserService.IsPro() && project.ProId != currentUserService.GetUserID())
			return Result<ProjectResponseDto>.Failure("Pro users can only update projects they created.", ResultErrorType.PermissionDenied);

		// If project is not open, only allow updating IsOpen to true
		if (!project.IsOpen && updateRequest.IsOpen != true)
			return Result<ProjectResponseDto>.Failure("Project is not in a state that can be updated.");

		var state = await repository
			.Query<State>()
			.ByAbbreviation(updateRequest.State)
			.FirstOrDefaultAsync(ct);

		if (state == null)
			return Result<ProjectResponseDto>.Failure("State not found.", ResultErrorType.NotFound);

		var zipCodeIsValid = await repository
			.Query<ZipCode>()
			.ByZipcode(updateRequest.ZipCode)
			.AnyAsync(ct);

		if (!zipCodeIsValid)
			return Result<ProjectResponseDto>.Failure("Invalid zip code.");

		var mapper = new ProjectMapper();
		mapper.UpdateProject(updateRequest, project);

		project.Title = updateRequest.Title ?? project.Title;
		project.Address.Line1 = updateRequest.AddressLine1 ?? project.Address.Line1;
		project.Address.Line2 = updateRequest.AddressLine2 ?? project.Address.Line2;
		project.Address.City = updateRequest.City ?? project.Address.City;
		project.Address.State = state ?? project.Address.State;
		project.Address.ZipCode = updateRequest.ZipCode ?? project.Address.ZipCode;
		project.DateUpdated = DateTimeOffset.UtcNow;
		project.Summary = updateRequest.Summary ?? project.Summary;

		// Only allow Pro, AccountManager, and SuperAdmin to update IsOpen
		if (currentUserService.IsPro() || currentUserService.IsAccountManager() || currentUserService.IsSuperAdmin())
		{
			project.IsOpen = updateRequest.IsOpen ?? project.IsOpen;
		}

		project.LeadID = updateRequest.LeadID ?? project.LeadID;
		project.SelectedPaintColors = updateRequest.SelectedPaintColors ?? project.SelectedPaintColors;
		project.ApproxSquareFootage = updateRequest.ApproxSquareFootage ?? project.ApproxSquareFootage;

		repository.AddOrUpdate(project);

		await repository.SaveAsync(ct);

		var projectResponse = ProjectMapper.MapToProjectResponseDto(project);

		return Result<ProjectResponseDto>.Success(projectResponse);
	}

	public async Task<Result> DeleteProject(int projectId, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		var project = await repository
			.Query<Project>()
			.ByProjectID(projectId)
			.Include(p => p.Address)
			.Include(p => p.Lead)
				.ThenInclude(l => l.Address)
			.FirstOrDefaultAsync(ct);

		if (project == null)
			return Result.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToProject(project, ct);

		if (!accessResult.IsSuccess)
			return Result.Failure(accessResult.Error!, accessResult.ErrorType);

		if (project.IsDeleted)
			return Result.Failure("Can't delete deleted project.");

		project.IsDeleted = true;
		project.DateDeleted = DateTimeOffset.UtcNow;

		repository.AddOrUpdate(project);
		await repository.SaveAsync(ct);

		return Result.Success();
	}

	public async Task<Result> RestoreProject(int projectId, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		var project = await repository
			.Query<Project>()
			.ByProjectID(projectId)
			.Include(p => p.Address)
			.Include(p => p.Lead)
				.ThenInclude(l => l.Address)
			.FirstOrDefaultAsync(ct);

		if (project == null)
			return Result.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToProject(project, ct);

		if (!accessResult.IsSuccess)
			return Result.Failure(accessResult.Error!, accessResult.ErrorType);

		if (!project.IsDeleted)
			return Result.Failure("Can't restore a project that is not deleted.");

		project.IsDeleted = false;
		project.DateDeleted = null;

		repository.AddOrUpdate(project);
		await repository.SaveAsync(ct);

		return Result.Success();
	}

	public async Task<Result<DetailedProjectResponseDto>> GetProject(int projectId, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		var currentUserId = currentUserService.GetUserID();

		var project = await repository
			.Query<Project>()
			.ByProjectID(projectId)
			.Include(p => p.Address)
				.ThenInclude(p => p.State)
			.Include(p => p.RequestedEstimates)
				.ThenInclude(re => re.Estimates
					.Where(e => !currentUserService.IsPro() || e.ProUserID == currentUserId))
					.ThenInclude(e => e.Job)
			.Include(p => p.RequestedEstimates)
				.ThenInclude(re => re.Estimates
					.Where(e => !currentUserService.IsPro() || e.ProUserID == currentUserId))
					.ThenInclude(e => e.ScheduledEstimate)
			.Include(p => p.RequestedEstimates)
				.ThenInclude(re => re.Estimates
					.Where(e => !currentUserService.IsPro() || e.ProUserID == currentUserId))
					.ThenInclude(e => e.ProUser)
						.ThenInclude(u => u.ProUser)
			.Include(p => p.RequestedEstimates)
				.ThenInclude(re => re.EstimateType)
			.Include(p => p.Lead)
			.FirstOrDefaultAsync(ct);

		if (project == null)
			return Result<DetailedProjectResponseDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToProject(project, ct);

		if (!accessResult.IsSuccess)
			return Result<DetailedProjectResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);

		if (currentUserService.IsPro() &&
			project.IsOpen && (project.ScheduledEstimates == null ||
			!project.ScheduledEstimates.Any(x => x.ProUserID == currentUserId && x.IsDeleted == false)))
		{
			project.IsOpen = false;
		}

		var detailedProjectResponseDto = ProjectMapper.MapToDetailedProjectResponseDto(project);

		var (estimates, jobs) = GetEstimatesAndJobsForDetailedProjectResponseDto(project);
		// Ensure newest items appear first in the lists
		detailedProjectResponseDto.Estimates = estimates
			.OrderByDescending(e => e.Id)
			.ToList();
		detailedProjectResponseDto.Jobs = jobs
			.OrderByDescending(j => j.Id)
			.ToList();

		return Result<DetailedProjectResponseDto>.Success(detailedProjectResponseDto);
	}

	private static (List<EstimateJobResponseDto> Estimates, List<EstimateJobResponseDto> Jobs) GetEstimatesAndJobsForDetailedProjectResponseDto(Project project)
	{
		var estimates = new List<EstimateJobResponseDto>();
		var jobs = new List<EstimateJobResponseDto>();

		if (project?.RequestedEstimates == null)
			return (estimates, jobs);

		foreach (var requestedEstimate in project.RequestedEstimates)
		{
			if (requestedEstimate.Estimates == null ||
				requestedEstimate.Estimates.Count == 0)
			{
				var providedBy = $"{requestedEstimate.ProUser?.ProUser?.BusinessName} ({requestedEstimate.ProUser?.GetFullName()})";

				estimates.Add(new EstimateJobResponseDto(
					requestedEstimate.RequestedEstimateID,
					null,
					$"Estimate: {requestedEstimate.Name}",
					providedBy,
					EstimateStatus.ReadyToBeScheduled.ToString().ToCamelCase(),
					false,
					true,
					null,
					null,
					null,
					null,
					EstimateJobType.RequestedEstimate,
					requestedEstimate.EstimateType.EstimateTypeName,
					null));
			}
			else foreach (var estimate in requestedEstimate.Estimates)
				{
					var name = $"{estimate.RequestedEstimate.Name}";
					var providedBy = $"{estimate.ProUser?.ProUser?.BusinessName} ({estimate.ProUser?.GetFullName()})";

					estimates.Add(new EstimateJobResponseDto(
						estimate.EstimateID,
						estimate.JobId,
						$"{(estimate.RequestedEstimate.IsChangeOrder == true ? "Change Order" : "Estimate")}: {name}",
						providedBy,
						estimate.Status.ToString().ToCamelCase(),
						estimate.IsOnHold,
						estimate.IsActive,
						estimate.Total,
						estimate.ScheduledEstimate?.ScheduledDateTime,
						null,
						null,
						estimate.RequestedEstimate.IsChangeOrder == true ?
						EstimateJobType.ChangeOrder : EstimateJobType.Estimate,
						requestedEstimate.EstimateType.EstimateTypeName,
						estimate.ScheduledEstimate?.ScheduledEstimateID));

					var job = estimate.Job;

					if (job != null)
						jobs.Add(new EstimateJobResponseDto(
							job.JobId,
							job.EstimateId,
							$"Job: {name}",
							providedBy,
							job.Status.ToString().ToCamelCase(),
							job.IsOnHold,
							job.IsActive,
							job.Estimate.Total,
							job.ScheduledDateWorkStarted,
							job.ScheduledDateWorkCompleted,
							null,
							EstimateJobType.Job,
							requestedEstimate.EstimateType.EstimateTypeName,
							null));
				}
		}

		return (estimates, jobs);
	}
}
