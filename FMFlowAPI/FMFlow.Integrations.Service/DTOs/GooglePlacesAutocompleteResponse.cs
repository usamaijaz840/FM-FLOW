namespace FMFlow.Integrations.Service.DTOs;

// See https://developers.google.com/maps/documentation/places/web-service/place-autocomplete for more information.
internal record GooglePlacesAutocompleteResponse
{
	public List<Suggestion> Suggestions { get; init; } = new();
}

// Supporting DTOs
internal record Suggestion
{
	public PlacePrediction? PlacePrediction { get; init; }
	public QueryPrediction? QueryPrediction { get; init; }
}

internal record PlacePrediction
{
	public string Place { get; init; } = string.Empty;
	public string PlaceId { get; init; } = string.Empty;
	public TextInfo Text { get; init; } = new();
	public StructuredFormat StructuredFormat { get; init; } = new();
	public List<string> Types { get; init; } = new();
}

internal record QueryPrediction
{
	public TextInfo Text { get; init; } = new();
}

internal record TextInfo
{
	public string Text { get; init; } = string.Empty;
	public List<Match> Matches { get; init; } = new();
}

internal record Match
{
	public int? StartOffset { get; init; }
	public int EndOffset { get; init; }
}

internal record StructuredFormat
{
	public SimpleText MainText { get; init; } = new();
	public SimpleText SecondaryText { get; init; } = new();
}

internal record SimpleText
{
	public string Text { get; init; } = string.Empty;
}
