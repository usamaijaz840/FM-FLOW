using FMFlow.Entities;
using FMFlow.Estimates.Interface.DTOs;

namespace FMFlow.Estimates.Service.Mappers;

public class JobMapper
{
	public static Job MapJobRequestToJob(JobRequestDto dto) =>
		new()
		{
			EstimateId = dto.EstimateId,
			Summary = dto.Summary,
			IsOnHold = dto.IsOnHold ?? false,
			IsActive = dto.IsActive ?? true,
			ScheduledDateWorkStarted = dto.ScheduledDateWorkStarted,
			ScheduledDateWorkCompleted = dto.ScheduledDateWorkCompleted,
			ActualDateWorkStarted = dto.ActualDateWorkStarted,
			ActualDateWorkCompleted = dto.ActualDateWorkCompleted,
			SignOffDate = dto.SignOffDate,
			NotifiedPriorToArrival = dto.NotifiedPriorToArrival,
			CooperativeWithCustomer = dto.CooperativeWithCustomer,
			CleanedUpWorkAreas = dto.CleanedUpWorkAreas,
			CompletedScopeOfWork = dto.CompletedScopeOfWork,
			ContractorWorkIsSatisfactory = dto.ContractorWorkIsSatisfactory,
			RateContractorPerformance = dto.RateContractorPerformance,
			SignOffComment = dto.SignOffComment
		};

	public static JobResponseDto MapJobToJobResponse(Job job) =>
		new(
			job.JobId,
			job.EstimateId,
			job.Summary,
			job.IsOnHold,
			job.IsActive,
			job.Status,
			job.ScheduledDateWorkStarted,
			job.ScheduledDateWorkCompleted,
			job.ActualDateWorkStarted,
			job.ActualDateWorkCompleted,
			job.SignOffDate,
			job.NotifiedPriorToArrival,
			job.CooperativeWithCustomer,
			job.CleanedUpWorkAreas,
			job.CompletedScopeOfWork,
			job.ContractorWorkIsSatisfactory,
			job.RateContractorPerformance,
			job.SignOffComment
			);

	public static DetailedJobResponseDto MapJobToDetailedJobResponse(Job job, int? proUserLogoFileId, bool hasJobCompletionImage) =>
		new(
			job.JobId,
			job.EstimateId,
			job.Estimate.Total,
			job.Estimate.RequestedEstimate.RequestedEstimateID,
			job.Estimate.RequestedEstimate.Name,
			job.Estimate.RequestedEstimate.EstimateType.EstimateTypeName,
			job.Estimate.ScheduledEstimateID,
			job.Summary,
			job.IsOnHold,
			job.IsActive,
			job.Status,
			job.ScheduledDateWorkStarted,
			job.ScheduledDateWorkCompleted,
			job.ActualDateWorkStarted,
			job.ActualDateWorkCompleted,
			job.SignOffDate,
			job.NotifiedPriorToArrival,
			job.CooperativeWithCustomer,
			job.CleanedUpWorkAreas,
			job.CompletedScopeOfWork,
			job.ContractorWorkIsSatisfactory,
			job.RateContractorPerformance,
			job.SignOffComment,
			hasJobCompletionImage,
			job.Estimate.RequestedEstimate.Project.ProjectID,
			job.Estimate.RequestedEstimate.Project.Title,
			job.Estimate.RequestedEstimate.Project.Summary,
			job.Estimate.RequestedEstimate.Project.Address.Line1,
			job.Estimate.RequestedEstimate.Project.Address.Line2,
			job.Estimate.RequestedEstimate.Project.Address.City,
			job.Estimate.RequestedEstimate.Project.Address.State.StateName,
			job.Estimate.RequestedEstimate.Project.Address.ZipCode,
			job.Estimate.ProUser.UserID,
			job.Estimate.ProUser.GetFullName(),
			job.Estimate.ProUser.Email,
			job.Estimate.ProUser?.ProUser?.BusinessName,
			proUserLogoFileId,
			job.Estimate.RequestedEstimate.Project.Lead.LeadID,
			job.Estimate.RequestedEstimate.Project.Lead.FirstName,
			job.Estimate.RequestedEstimate.Project.Lead.LastName,
			job.Estimate.RequestedEstimate.Project.Lead.OrganizationName,
			job.Estimate.RequestedEstimate.Project.Lead.PhoneNumber,
			job.Estimate.RequestedEstimate.Project.Lead.Mobile,
			job.Estimate.RequestedEstimate.Project.Lead.Email,
			job.Estimate.RequestedEstimate.Project.Lead.Address.Line1,
			job.Estimate.RequestedEstimate.Project.Lead.Address.Line2,
			job.Estimate.RequestedEstimate.Project.Lead.Address.City,
			job.Estimate.RequestedEstimate.Project.Lead.Address.State.StateName,
			job.Estimate.RequestedEstimate.Project.Lead.Address.ZipCode
			);

	public static JobSignOffResponseDto MapJobToJobSignOffResponse(Job job) =>
		new(
			job.SignOffDate,
			job.NotifiedPriorToArrival,
			job.CooperativeWithCustomer,
			job.CleanedUpWorkAreas,
			job.CompletedScopeOfWork,
			job.ContractorWorkIsSatisfactory,
			job.RateContractorPerformance,
			job.SignOffComment,
			job.ActualDateWorkStarted,
			job.ActualDateWorkCompleted
			);

	public static KanbanEstimateOrJobResponseDto MapToKanbanDto(Job source) => new(
			source.JobId,
			source.Status.ToString(),
			source.Estimate.RequestedEstimate.Name,
			source.Estimate.RequestedEstimate.Project.Address.ToString(),
			source.Estimate.ProUser.GetFullName(),
			source.Estimate.Total,
			null,
			source.Estimate.RequestedEstimate.EstimateType.EstimateTypeName,
			source.Estimate.RequestedEstimate.Project.Lead != null ? $"{source.Estimate.RequestedEstimate.Project.Lead.FirstName} {source.Estimate.RequestedEstimate.Project.Lead.LastName}" : null);
}
