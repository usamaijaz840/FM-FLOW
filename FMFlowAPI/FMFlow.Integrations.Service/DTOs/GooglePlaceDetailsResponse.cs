namespace FMFlow.Integrations.Service.DTOs;

// See https://developers.google.com/maps/documentation/places/web-service/place-details for more information.

internal record GooglePlaceDetailsResponse
{
	public string FormattedAddress { get; init; } = string.Empty;
	public List<AddressComponent> AddressComponents { get; init; } = new();
}

// Supporting DTOs
internal record AddressComponent
{
	public string LongText { get; init; } = string.Empty;
	public string ShortText { get; init; } = string.Empty;
	public List<string> Types { get; init; } = new();
}
