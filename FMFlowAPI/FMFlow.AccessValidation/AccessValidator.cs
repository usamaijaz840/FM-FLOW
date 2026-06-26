using EFRepository;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Identity.Interface;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.AccessValidation;

public class AccessValidator(ICurrentUserService currentUserService, IRepository repository) : IAccessValidator
{
	private async Task<Result> ValidateAccess<T>(
		int? entityId,
		T? entity,
		Func<IQueryable<T>, IQueryable<T>> includeQuery,
		Func<T, string> getZipCode,
		Func<T, int?> getProUserId,
		Func<T, int?>? getCustomerUserId,
		Func<T, Lead?>? getLead,
		CancellationToken ct) where T : class, new()
	{
		// Fetch the entity if it's not provided
		if (entity == null && entityId.HasValue)
		{
			entity = await includeQuery(repository.Query<T>())
				.FirstOrDefaultAsync(ct);
		}

		if (entity == null)
		{
			return Result.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);
		}

		if (currentUserService.IsSuperAdmin() || currentUserService.IsChatBot())
		{
			return Result.Success();
		}

		if (currentUserService.IsEstimateRecipient())
		{
			// EstimateRecipient access: can only view estimates and create payments for estimates they're assigned to
			int recipientId = currentUserService.GetUserID();

			if (entity is Estimate estimate)
			{
				bool hasAccess = await repository.Query<EstimateRecipient>()
					.Where(er => er.EstimateRecipientId == recipientId && er.EstimateId == estimate.EstimateID && !er.IsDeleted)
					.AnyAsync(ct);

				if (hasAccess)
				{
					return Result.Success();
				}
			}

			// Deny access to all other entity types (Projects, Leads, Files, etc.)
			return Result.Failure(ErrorMessages.ResourceAccessDenied, ResultErrorType.PermissionDenied);
		}

		var currentUser = await currentUserService.GetCurrentUser(ct);
		var currentUserId = currentUserService.GetUserID();

		if (currentUser == null)
		{
			return Result.Failure(ErrorMessages.UserNotFound, ResultErrorType.NotFound);
		}


		if (currentUserService.IsAccountManager())
		{
			var assignedZipCodes = await repository
				.Query<EmployeeUser>()
				.ByUserID(currentUser.UserID)
				.Include(eu => eu.AssignedZipCodes)
				.SelectMany(eu => eu.AssignedZipCodes.Select(z => z.Zipcode))
				.ToListAsync(ct);

			if (assignedZipCodes == null || assignedZipCodes.Count == 0)
			{
				return Result.Failure(ErrorMessages.ResourceAccessDenied, ResultErrorType.PermissionDenied);
			}

			var hasZipAccess = assignedZipCodes.Contains(getZipCode(entity));

			IEnumerable<string> projectZipCodes = [];

			if (!hasZipAccess && entity is Lead lead)
			{
				// If projects and their addresses are already loaded use them, otherwise fetch them
				if (lead.Projects != null && lead.Projects.Count > 0 && lead.Projects.First().Address != null)
				{
					projectZipCodes = lead.Projects
						.Select(p => p.Address?.ZipCode)
						.Where(z => !string.IsNullOrWhiteSpace(z))!
						.Cast<string>();
				}
				else
				{
					projectZipCodes = await repository.Query<Project>()
						.Where(p => p.LeadID == lead.LeadID && !p.IsDeleted)
						.Select(p => p.Address.ZipCode)
						.Distinct()
						.ToListAsync(ct);
				}
			}

			if (!hasZipAccess && entity is Estimate estimate)
			{
				projectZipCodes = await repository.Query<Project>()
						.Where(p => p.ProjectID == estimate.RequestedEstimate.ProjectID)
						.Select(p => p.Address.ZipCode)
						.Distinct()
						.ToListAsync(ct);
			}

			if (!hasZipAccess && entity is RequestedEstimate requestedEstimate)
			{
				projectZipCodes = await repository.Query<Project>()
						.Where(p => p.ProjectID == requestedEstimate.ProjectID)
						.Select(p => p.Address.ZipCode)
						.Distinct()
						.ToListAsync(ct);
			}

			if (projectZipCodes.Any() && projectZipCodes.Any(assignedZipCodes.Contains))
			{
				hasZipAccess = true;
			}

			if (!hasZipAccess)
			{
				return Result.Failure(ErrorMessages.ResourceAccessDenied, ResultErrorType.PermissionDenied);
			}
		}
		else if (currentUserService.IsPro())
		{
			if (getProUserId(entity) == currentUserId)
			{
				return Result.Success();
			}
			else if (entity is Lead lead)
			{
				// A pro can access a lead if either is assigned to him (manual lead)
				// or one of its projects has an estimate or requested estimate assigned to him

				if (lead == null || lead.Projects == null || lead.Projects.Count == 0)
				{
					return Result.Failure(ErrorMessages.ResourceAccessDenied, ResultErrorType.PermissionDenied);
				}

				foreach (var leadProject in lead.Projects)
				{
					ct.ThrowIfCancellationRequested();

					if (await ProHasEstimateOrRequestedEstimateAssigned(leadProject, currentUserId, ct))
					{
						return Result.Success();
					}
				}

				return Result.Failure(ErrorMessages.ResourceAccessDenied, ResultErrorType.PermissionDenied);
			}

			var assignedZipCodes = await repository.Query<ProUserToProZipcode>()
				.ByUserID(currentUser.UserID)
				.Select(x => x.Zipcode)
				.ToListAsync(ct);

			if (assignedZipCodes == null || !assignedZipCodes.Contains(getZipCode(entity)))
			{
				return Result.Failure(ErrorMessages.ResourceAccessDenied, ResultErrorType.PermissionDenied);
			}

			if (entity is Project project)
			{
				if (project.ProId == currentUserId)
				{
					return Result.Success();
				}

				if (await ProHasEstimateOrRequestedEstimateAssigned(project, currentUserId, ct))
				{
					return Result.Success();
				}
			}

			return Result.Failure(ErrorMessages.ResourceAccessDenied, ResultErrorType.PermissionDenied);
		}
		else if (currentUserService.IsCustomer() && getCustomerUserId != null)
		{
			if (getCustomerUserId(entity) != currentUserId)
			{
				return Result.Failure(ErrorMessages.ResourceAccessDenied, ResultErrorType.PermissionDenied);
			}
		}
		else if (currentUserService.IsScheduler() && getLead != null)
		{
			var lead = getLead(entity);

			if (lead == null)
			{
				return Result.Failure(ErrorMessages.ResourceAccessDenied, ResultErrorType.PermissionDenied);
			}

		ct.ThrowIfCancellationRequested();

		// var scheduledCount = await repository.Query<ScheduledEstimate>()
		// 	.Where(se => !se.IsDeleted && se.Project.LeadID == lead.LeadID)
		// 	.CountAsync(ct);

		if (lead == null || (lead.SchedulerId != null && lead.SchedulerId != currentUserId) /* || scheduledCount >= 3 */)
		{
			return Result.Failure(ErrorMessages.ResourceAccessDenied, ResultErrorType.PermissionDenied);
		}

			ct.ThrowIfCancellationRequested();
		}
		else if (currentUserService.IsTempCustomer())
		{
			if (getCustomerUserId != null && getCustomerUserId(entity) != currentUserId)
			{
				return Result.Failure(ErrorMessages.ResourceAccessDenied, ResultErrorType.PermissionDenied);
			}

			if (getLead != null)
			{
				var lead = getLead(entity);
				if (lead == null)
				{
					return Result.Failure(ErrorMessages.ResourceAccessDenied, ResultErrorType.PermissionDenied);
				}

				var countOfLeadsMatchCurrentUser = await repository.Query<Lead>()
					.Where(l => l.CustomerID == currentUser.UserID)
					.CountAsync();

				if (countOfLeadsMatchCurrentUser == 0 || lead.CustomerID != currentUser.UserID)
				{
					return Result.Failure(ErrorMessages.ResourceAccessDenied, ResultErrorType.PermissionDenied);
				}
			}
		}
		else
		{
			return Result.Failure(ErrorMessages.ResourceAccessDenied, ResultErrorType.PermissionDenied);
		}

		return Result.Success();
	}

	private async Task<bool> ProHasEstimateOrRequestedEstimateAssigned(Project project, int currentUserId, CancellationToken ct)
	{
		var proHasRequestedEstimateAssigned = project.RequestedEstimates.Any(e => e.ProUserID == currentUserId);

		if (proHasRequestedEstimateAssigned)
		{
			return true;
		}

		var proHasEstimateAssigned = await repository.Query<RequestedEstimate>()
			.ByProjectID(project.ProjectID)
			.Where(re => re.Estimates.Any(e => e.ProUserID == currentUserId))
			.AnyAsync(ct);

		return proHasEstimateAssigned;
	}

	public async Task<Result> ValidateAccessToProject(int projectID, CancellationToken ct) =>
		await ValidateAccess<Project>(
			projectID,
			null,
			query => query.Include(p => p.Lead).Include(l => l.Address).ByProjectID(projectID),
			project => project.Address.ZipCode,
			project => project.Lead.ProUserID,
			project => project.Lead.CustomerID,
			project => project.Lead,
			ct);

	public async Task<Result> ValidateAccessToProject(Project project, CancellationToken ct) =>
		await ValidateAccess(
			null,
			project,
			query => query.Include(p => p.Lead).ThenInclude(l => l.Address),
			project => project.Address.ZipCode,
			project => project.Lead.ProUserID,
			project => project.Lead.CustomerID,
			project => project.Lead,
			ct);

	public async Task<Result> ValidateAccessToRequestedEstimate(int requestedEstimateId, CancellationToken ct) =>
		await ValidateAccess<RequestedEstimate>(
			requestedEstimateId,
			null,
			query => query.Include(re => re.Project).ThenInclude(p => p.Lead).ThenInclude(l => l.Address).ByRequestedEstimateID(requestedEstimateId),
			requestedEstimate => requestedEstimate.Project.Lead.Address.ZipCode,
			requestedEstimate => requestedEstimate.ProUserID,
			requestedEstimate => requestedEstimate.Project.Lead.CustomerID,
			requestedEstimate => requestedEstimate.Project.Lead,
			ct);

	public async Task<Result> ValidateAccessToRequestedEstimate(RequestedEstimate requestedEstimate, CancellationToken ct) =>
		await ValidateAccess(
			null,
			requestedEstimate,
			query => query.Include(re => re.Project).ThenInclude(p => p.Lead).ThenInclude(l => l.Address),
			requestedEstimate => requestedEstimate.Project.Lead.Address.ZipCode,
			requestedEstimate => requestedEstimate.ProUserID,
			requestedEstimate => requestedEstimate.Project.Lead.CustomerID,
			requestedEstimate => requestedEstimate.Project.Lead,
			ct);

	public async Task<Result> ValidateAccessToProject(ScheduledEstimate scheduledEstimate, CancellationToken ct) =>
		await ValidateAccess<Project>(
			scheduledEstimate.ProjectID,
			null,
			query => query.Include(p => p.Lead).ThenInclude(l => l.Address).ByProjectID(scheduledEstimate.ProjectID),
			project => project.Lead.Address.ZipCode,
			project => project.Lead.ProUserID,
			project => project.Lead.CustomerID,
			project => project.Lead,
			ct);

	public async Task<Result> ValidateAccessToEstimate(int estimateId, CancellationToken ct) =>
	await ValidateAccess<Estimate>(
		estimateId,
		null,
		query => query
			.Include(e => e.RequestedEstimate)
				.ThenInclude(re => re.Project)
					.ThenInclude(p => p.Lead)
						.ThenInclude(l => l.Address)
			.Where(e => e.EstimateID == estimateId),
		estimate => estimate.RequestedEstimate.Project.Lead.Address.ZipCode,
		estimate => estimate.ProUserID,
		estimate => estimate.RequestedEstimate.Project.Lead.CustomerID,
		estimate => estimate.RequestedEstimate.Project.Lead,
		ct);

	public async Task<Result> ValidateAccessToEstimate(Estimate estimate, CancellationToken ct) =>
		await ValidateAccess(
			null,
			estimate,
			query => query.Include(re => re.RequestedEstimate).ThenInclude(re => re.Project).ThenInclude(p => p.Lead).ThenInclude(l => l.Address).ByEstimateID(estimate.EstimateID),
			estimate => estimate.RequestedEstimate.Project.Lead.Address.ZipCode,
			estimate => estimate.ProUserID,
			estimate => estimate.RequestedEstimate.Project.Lead.CustomerID,
			estimate => estimate.RequestedEstimate.Project.Lead,
			ct);

	public async Task<Result> ValidateAccessToEstimateFile(FileItemToEstimate fileEstimate, CancellationToken ct) =>
		await ValidateAccess(
			null,
			fileEstimate,
			query => query.ByFileID(fileEstimate.FileID).ByEstimateID(fileEstimate.EstimateID),
			fileToEstimate => fileToEstimate.Estimate.RequestedEstimate.Project.Lead.Address.ZipCode,
			fileToEstimate => fileToEstimate.Estimate.ProUserID,
			fileToEstimate => fileToEstimate.Estimate.RequestedEstimate.Project.Lead.CustomerID,
			null,
			ct);

	public async Task<Result> ValidateAccessToLead(Lead lead, CancellationToken ct) =>
		await ValidateAccess(
			null,
			lead,
			query => query,
			lead => lead.Address.ZipCode,
			lead => lead.ProUserID,
			lead => lead.CustomerID,
			lead => lead,
			ct);
}
