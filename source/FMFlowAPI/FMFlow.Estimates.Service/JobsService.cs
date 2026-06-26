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
using FMFlow.LeadTimelines.Interface;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using static FMFlow.Entities.FileItemToEstimate;

namespace FMFlow.Estimates.Service;

public class JobsService(
	IRepository repository,
	ApplicationDbContext dbContext,
	ICurrentUserService currentUserService,
	IAccessValidator accessValidator,
	ILeadTimelineService leadTimelineService,
	IJobCompletionService jobCompletionService,
	IValidator<JobRequestDto> validator,
	IEmailSenderService emailSenderService) : IJobsService
{
	public async Task<Result<JobResponseDto>> CreateJob(JobRequestDto request, CancellationToken ct)
	{
		var requestValidation = await DtoValidator.Validate(request, validator, ct);

		if (!requestValidation.IsSuccess)
			return Result<JobResponseDto>.Failure(requestValidation.Error!);

		var accessResult = await accessValidator.ValidateAccessToEstimate(request.EstimateId, ct);

		if (!accessResult.IsSuccess)
			return Result<JobResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);

		var nonDeletedJobAlreadyExists = await repository.Query<Job>()
			.ByEstimateId(request.EstimateId)
			.ByIsDeleted(false)
			.AnyAsync(ct);

		if (nonDeletedJobAlreadyExists)
			return Result<JobResponseDto>.Failure("A non-deleted job already exists for this estimate.", ResultErrorType.BadRequest);

		var job = JobMapper.MapJobRequestToJob(request);

		job.ScheduledDateWorkStarted = request.ScheduledDateWorkStarted.ToUniversalTime();

		job.Estimate = await repository.Query<Estimate>()
			.ByEstimateID(request.EstimateId)
			.Include(e => e.ProUser)
				.ThenInclude(p => p.ProUser)
			.Include(e => e.RequestedEstimate)
				.ThenInclude(re => re.Project)
					.ThenInclude(p => p.Lead)
						.ThenInclude(l => l.Customer)
			.FirstAsync(ct);

		await leadTimelineService.RecordLeadTimelineAsync(job, TimelineEventKey.JobScheduled, ct);

		repository.AddNew(job);
		await repository.SaveAsync(ct);

		if (job.Estimate.RequestedEstimate.Project.Lead.CustomerType == CustomerType.LocalCommercial)
		{
			await emailSenderService.SendEmailCustomerJobScheduled(job, ct);
		}
		else
		{
			await emailSenderService.SendEmailCustomerResidentialJobScheduled(job, ct);
		}

		var jobResponse = JobMapper.MapJobToJobResponse(job);

		return Result<JobResponseDto>.Success(jobResponse);
	}

	public async Task<Result<JobResponseDto>> UpdateJob(int jobId, JobUpdateRequestDto request, CancellationToken ct)
	{
		var job = await repository.Query<Job>()
			.ByJobId(jobId)
			.Include(j => j.Estimate)
				.ThenInclude(e => e.ProUser)
			.ByIsDeleted(false)
			.FirstOrDefaultAsync(ct);

		if (job == null)
			return Result<JobResponseDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToEstimate(job.EstimateId, ct);

		if (!accessResult.IsSuccess)
			return Result<JobResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);

		if (!job.IsOnHold && request.IsOnHold.HasValue && request.IsOnHold.Value)
			await leadTimelineService.RecordLeadTimelineAsync(job, TimelineEventKey.JobOnHold, ct, request.StatusUpdateReason);

		job.Summary = request.Summary ?? job.Summary;
		job.IsOnHold = request.IsOnHold ?? job.IsOnHold;
		job.IsActive = request.IsActive ?? job.IsActive;
		job.ScheduledDateWorkStarted = request.ScheduledDateWorkStarted.HasValue
			? request.ScheduledDateWorkStarted.Value.ToUniversalTime()
			: job.ScheduledDateWorkStarted;
		job.ScheduledDateWorkCompleted = request.ScheduledDateWorkCompleted ?? job.ScheduledDateWorkCompleted;

		var jobSetToInProgress = false;

		if (request.Status != null && request.Status != job.Status)
		{
			if ((int)request.Status < (int)job.Status)
				return Result<JobResponseDto>.Failure("Cannot move job status backwards.");

			if ((int)request.Status > (int)job.Status + 1)
				return Result<JobResponseDto>.Failure("Cannot skip job statuses.");

			// to do: finish validation for closing jobs once we can check conditions
			if (request.Status == JobStatus.Closed)
			{
				await leadTimelineService.RecordLeadTimelineAsync(job, TimelineEventKey.JobClosed, ct, request.StatusUpdateReason);

				return Result<JobResponseDto>.Failure("Before closing a job, these conditions must be met: photos were uploaded, contract signed by both the customer and pro was uploaded, and estimate has been fully paid. IMPLEMENTATION PENDING.");
			}

			job.Status = request.Status.Value;
			job.StatusLastUpdate = DateTimeOffset.UtcNow;

			if (job.Status == JobStatus.InProgress)
			{
				jobSetToInProgress = true;
			}
		}

		repository.AddOrUpdate(job);
		await repository.SaveAsync(ct);

		if (jobSetToInProgress)
		{
			var estimate = await repository.Query<Estimate>()
				.ByEstimateID(job.EstimateId)
				.Include(e => e.ProUser)
				.Include(e => e.RequestedEstimate)
					.ThenInclude(re => re.Project)
						.ThenInclude(p => p.Lead)
							.ThenInclude(l => l.Customer)
			.FirstOrDefaultAsync(ct);

			await emailSenderService.SendEmailCustomerEstimatorArrived(estimate!, ct);
		}

		var jobResponse = JobMapper.MapJobToJobResponse(job);
		return Result<JobResponseDto>.Success(jobResponse);
	}

	public async Task<Result<DetailedJobResponseDto>> GetJob(int jobId, CancellationToken ct)
	{
		var job = await repository.Query<Job>()
			.ByJobId(jobId)
			.Include(j => j.Estimate)
				.ThenInclude(e => e.RequestedEstimate)
					.ThenInclude(re => re.Project)
						.ThenInclude(p => p.Lead)
							.ThenInclude(l => l.Address)
								.ThenInclude(a => a.State)
			.Include(j => j.Estimate)
				.ThenInclude(e => e.RequestedEstimate)
					.ThenInclude(re => re.Project)
						.ThenInclude(p => p.Address)
							.ThenInclude(a => a.State)
			.Include(j => j.Estimate)
				.ThenInclude(e => e.ProUser)
					.ThenInclude(pro => pro.ProUser)
			.Include(j => j.Estimate)
				.ThenInclude(e => e.RequestedEstimate)
					.ThenInclude(re => re.Project)
			.Include(j => j.Estimate)
				.ThenInclude(e => e.RequestedEstimate)
					.ThenInclude(re => re.EstimateType)
			.ByIsDeleted(false)
			.FirstOrDefaultAsync(ct);

		if (job == null)
			return Result<DetailedJobResponseDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToEstimate(job.EstimateId, ct);

		if (!accessResult.IsSuccess)
			return Result<DetailedJobResponseDto>.Failure(accessResult.Error!, accessResult.ErrorType);

		int? proUserLogoFileId = await repository.Query<ProUserFile>()
			.ByUserID(job.Estimate.ProUserID)
			.Where(puf => puf.ProFileType == ProUserFileType.Logo)
			.Select(pf => pf.FileID)
			.FirstOrDefaultAsync(ct);

		var hasJobCompletionImage = await repository.Query<FileItemToEstimate>()
			.ByEstimateID(job.EstimateId)
			.ByIsDeleted(false)
			.Where(f => f.FileType == EstimateFileType.JobCompletionImage)
			.AnyAsync(ct);

		var jobResponse = JobMapper.MapJobToDetailedJobResponse(job, proUserLogoFileId, hasJobCompletionImage);

		return Result<DetailedJobResponseDto>.Success(jobResponse);
	}

	public async Task<Result<JobSignOffResponseDto>> CreateJobSignOff(int jobId, JobSignOffRequestDto request, CancellationToken ct)
	{
		var job = await repository.Query<Job>()
			.ByJobId(jobId)
			.Include(j => j.Estimate)
				.ThenInclude(e => e.ProUser)
			.Include(j => j.Estimate)
				.ThenInclude(e => e.RequestedEstimate)
					.ThenInclude(re => re.Project)
						.ThenInclude(p => p.Lead)
							.ThenInclude(l => l.Customer)
			.ByIsDeleted(false)
			.FirstOrDefaultAsync(ct);

		if (job == null)
			return Result<JobSignOffResponseDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		job.SignOffDate = request.SignOffDate;
		job.ActualDateWorkStarted = request.ActualDateWorkStarted;
		job.ActualDateWorkCompleted = request.ActualDateWorkCompleted;
		job.NotifiedPriorToArrival = request.NotifiedPriorToArrival;
		job.CooperativeWithCustomer = request.CooperativeWithCustomer;
		job.CleanedUpWorkAreas = request.CleanedUpWorkAreas;
		job.CompletedScopeOfWork = request.CompletedScopeOfWork;
		job.ContractorWorkIsSatisfactory = request.ContractorWorkIsSatisfactory;
		job.RateContractorPerformance = request.RateContractorPerformance;
		job.SignOffComment = request.SignOffComment;
		job.Status = JobStatus.PendingCompletion;
		job.StatusLastUpdate = DateTimeOffset.UtcNow;

		repository.AddOrUpdate(job);

		await repository.SaveAsync(ct);

		await jobCompletionService.CloseJobIfEligible(job.JobId, ct);

		await emailSenderService.SendEmailCustomerSignOffSuccessful(job.Estimate, ct);

		var jobSignOffResponse = JobMapper.MapJobToJobSignOffResponse(job);

		return Result<JobSignOffResponseDto>.Success(jobSignOffResponse);
	}

	public async Task<Result<Dictionary<string, SearchResult<KanbanEstimateOrJobResponseDto>>>> SearchJobsForKanban(
		KanbanJobStatus? jobStatus,
		int? proUserId,
		int pageIndex,
		int pageSize,
		CancellationToken ct)
	{
		if (pageIndex < 0 || pageSize < 1)
			return Result<Dictionary<string, SearchResult<KanbanEstimateOrJobResponseDto>>>.Failure("Invalid pagination parameters.");

		pageSize = Math.Min(pageSize, 50);

		ct.ThrowIfCancellationRequested();

		var minRowNum = pageIndex * pageSize + 1;
		var maxRowNum = minRowNum + pageSize;

		var filters = new List<string>
		{
			"""re."IsDeleted" = FALSE""",
			"""p."IsDeleted" = FALSE""",
			"""j."IsActive" = TRUE""",
			"""j."StatusLastUpdate" > date_subtract(NOW(), '30 days')"""
		};

		var parameters = new DynamicParameters();

		parameters.Add("minRowNum", minRowNum);
		parameters.Add("maxRowNum", maxRowNum);

		if (jobStatus is not null)
		{
			filters.Add("""(CASE j."IsOnHold" WHEN TRUE THEN 'OnHold' ELSE j."Status" END) = :status""");
			parameters.Add("status", jobStatus.ToString());
		}

		if (currentUserService.IsPro())
		{
			// Scope to the current pro viewer
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
					return Result<Dictionary<string, SearchResult<KanbanEstimateOrJobResponseDto>>>.Success([]);

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
			WITH ranked_jobs AS
			(
			    SELECT
			       j."JobId" AS "Id"
			      ,(CASE j."IsOnHold" WHEN TRUE THEN 'OnHold' ELSE j."Status" END) AS "Status"
			      ,re."Name" AS "Name"
			      ,CONCAT(
			          a."Line1" || ', '
			         ,a."Line2" || ', '
			         ,a."City" || ', '
			         ,a."StateId" || ', '
			         ,a."ZipCode") AS "ProjectAddress"
			      ,CONCAT(fu."FirstName" || ' ', fu."LastName") AS "ProFullName"
			      ,CONCAT(l."FirstName" || ' ', l."LastName") AS "CustomerName"
			      ,e."Total" AS "Amount"
				  ,NULL::boolean AS "DepositHasBeenPaid"
						,et."EstimateTypeName" AS "EstimateTypeName"
			      ,ROW_NUMBER() OVER (PARTITION BY (CASE j."IsOnHold" WHEN TRUE THEN 'OnHold' ELSE j."Status" END) ORDER BY j."DateCreated" DESC) AS rn
			      ,COUNT(*) OVER (PARTITION BY (CASE j."IsOnHold" WHEN TRUE THEN 'OnHold' ELSE j."Status" END)) AS "StatusTotal"
			    FROM
			      "Jobs" j
			      JOIN "Estimates" e ON j."EstimateId" = e."EstimateID"
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
			   rj."Id"
			  ,rj."Status"
			  ,rj."Name"
			  ,rj."ProjectAddress"
			  ,rj."ProFullName"
			  ,rj."CustomerName"
			  ,rj."Amount"
			  ,rj."DepositHasBeenPaid"
				,rj."EstimateTypeName"
			  ,rj."StatusTotal"
			FROM
			  ranked_jobs rj
			WHERE
			  rj.rn >= :minRowNum
			  AND rj.rn < :maxRowNum;
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
				null,
				r.EstimateTypeName,
				r.CustomerName
			)), (int)(g.FirstOrDefault()?.StatusTotal ?? 0)));


		ct.ThrowIfCancellationRequested();

		return Result<Dictionary<string, SearchResult<KanbanEstimateOrJobResponseDto>>>.Success(results);
	}
}
