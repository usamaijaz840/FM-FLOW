using FMFlow.Entities;
using FMFlow.Estimates.Interface.DTOs;
using Riok.Mapperly.Abstractions;

namespace FMFlow.Estimates.Service.Mapper;

[Mapper]
public partial class ScheduledEstimateMapper
{
	public partial ScheduledEstimate MapToScheduledEstimate(ScheduledEstimateRequestDto request);

	public static ScheduledEstimateResponseDto MapToScheduledEstimateResponse(ScheduledEstimate scheduledEstimate) =>
		new(
			scheduledEstimate.ScheduledEstimateID,
			scheduledEstimate.ProjectID,
			scheduledEstimate.ProUserID,
			scheduledEstimate.ProUser?.ProUser?.BusinessName ?? string.Empty,
			scheduledEstimate.ProUser?.GetFullName() ?? string.Empty,
			scheduledEstimate.ScheduledDateTime
		);
}
