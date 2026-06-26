using FMFlow.Entities;

namespace FMFlow.LeadTimelines.Interface.DTOs;

public record LeadTimelineTemplate(TimelineEventKey timelineEventKey)
{
	public string EventKeyAsString =>
		$"timeline-events|event-name|{eventKeyAsSnakeCase}";

	public string EventDescriptionKeyAsString =>
		$"timeline-events|event-description|{eventKeyAsSnakeCase}";

	private string eventKeyAsSnakeCase => string.Concat(
			timelineEventKey.ToString().Select((c, i) =>
				i > 0 && char.IsUpper(c) ? "_" + char.ToLower(c) : char.ToLower(c).ToString()
			)
		);
}
