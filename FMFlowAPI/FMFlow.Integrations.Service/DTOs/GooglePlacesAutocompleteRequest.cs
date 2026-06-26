namespace FMFlow.Integrations.Service.DTOs;

// See https://developers.google.com/maps/documentation/places/web-service/place-autocomplete for more information.

internal record GooglePlacesAutocompleteRequest
{
	public string Input { get; init; } = string.Empty;
	public LocationBias? LocationBias { get; init; }
	public LocationRestriction? LocationRestriction { get; init; }
	public Origin? Origin { get; init; }
	public string? RegionCode { get; init; }
	public string? LanguageCode { get; init; }
	public List<string>? IncludedPrimaryTypes { get; init; }
	public bool IncludeQueryPredictions { get; init; } = false;
	public List<string>? IncludedRegionCodes { get; internal init; }
	public string? SessionToken { get; init; }
}

// Supporting DTOs
internal record LocationBias
{
	public Circle? Circle { get; init; }
	public Rectangle? Rectangle { get; init; }
}

internal record LocationRestriction
{
	public Circle? Circle { get; init; }
	public Rectangle? Rectangle { get; init; }
}

internal record Circle
{
	public Center Center { get; init; } = new();
	public double Radius { get; init; }
}

internal record Center
{
	public double Latitude { get; init; }
	public double Longitude { get; init; }
}

internal record Rectangle
{
	public Center Low { get; init; } = new();
	public Center High { get; init; } = new();
}

internal record Origin
{
	public double Latitude { get; init; }
	public double Longitude { get; init; }
}
