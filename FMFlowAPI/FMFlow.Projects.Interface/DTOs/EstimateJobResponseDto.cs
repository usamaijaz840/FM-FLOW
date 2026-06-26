namespace FMFlow.Projects.Interface.DTOs;

public record EstimateJobResponseDto(
	int Id,
	int? RelatedEstimateOrJobId,
	string Name,
	string ProvidedBy,
	string Status,
	bool IsOnHold,
	bool IsActive,
	decimal? Amount,
	DateTimeOffset? StartDateTime,
	DateOnly? FinishDate, // For Jobs
	DateTimeOffset? FinishDateTime, // For Estimates and Change Orders
	EstimateJobType Type,
	string EstimateTypeName,
	int? ScheduledEstimateId);

public enum EstimateJobType
{
	Job,
	Estimate,
	ChangeOrder,
	RequestedEstimate
}
