namespace FMFlow.Common;

public record SearchResult<T>(IEnumerable<T> Results, int TotalResults);
