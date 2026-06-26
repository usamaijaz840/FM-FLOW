using System.Text.Json;

namespace FMFlow.LeadTimelines.Interface.DTOs;

public record LeadTimelineResponseDto(
	int TimelineId,
	int LeadId,
	int UserId,
	string UserName,
	string EventNameKey,
	string EventKey,
	JsonDocument EventParameters,
	DateTimeOffset DateCreated);
