using EFRepository;
using FluentValidation;
using FMFlow.Entities;
using FMFlow.Estimates.Interface;
using Microsoft.EntityFrameworkCore;
using static FMFlow.Entities.FileItemToEstimate;

namespace FMFlow.Estimates.Service;

public class JobCompletionService(IRepository repository) : IJobCompletionService
{
	public async Task CloseJobIfEligible(int jobId, CancellationToken ct)
	{
		// Use AsNoTracking to ensure we get fresh data from database after trigger updates
		var job = await repository
			.Query<Job>()
			.ByJobId(jobId)
			.Include(j => j.Estimate)
			.AsNoTracking()
			.FirstOrDefaultAsync(ct);

		if (job == null)
			return;

		if (!job.Estimate.HasBeenPaid)
			return;
		
		if (job.Status != JobStatus.PendingCompletion)
			return;

		var requiredFileTypes = new[]
		{
			EstimateFileType.ProSignature,
			EstimateFileType.CustomerSignature,
			EstimateFileType.JobCompletionImage
		};

		var existingTypes = await repository
			.Query<FileItemToEstimate>()
			.ByEstimateID(job.Estimate.EstimateID)
			.Where(f => !f.IsDeleted)
			.Select(f => f.FileType)
			.Distinct()
			.ToListAsync(ct);

		if (!requiredFileTypes.All(existingTypes.Contains))
			return;
		
		// Reload the job for tracking since we used AsNoTracking above
		var jobToUpdate = await repository
			.Query<Job>()
			.ByJobId(jobId)
			.FirstOrDefaultAsync(ct);

		if (jobToUpdate != null)
		{
			jobToUpdate.Status = JobStatus.Closed;
			repository.AddOrUpdate(jobToUpdate);
			await repository.SaveAsync(ct);
		}
	}
}
