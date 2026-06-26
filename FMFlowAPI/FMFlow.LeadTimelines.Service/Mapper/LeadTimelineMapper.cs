using FMFlow.Entities;
using FMFlow.LeadTimelines.Interface.DTOs;

namespace FMFlow.LeadTimelines.Service.Mapper;

public static class LeadTimelineMapper
{
	public static List<LeadTimelineResponseDto> MapToLeadTimelineResponseDtos(List<LeadTimeline> Timelines)
	{
		return [.. Timelines.Select(x => new LeadTimelineResponseDto(
			x.TimelineId,
			x.LeadId,
			x.UserId,
			$"{x.User.GetFullName()}",
			x.EventNameKey,
			x.EventKey,
			x.EventParameters,
			x.DateCreated))];
	}
}
