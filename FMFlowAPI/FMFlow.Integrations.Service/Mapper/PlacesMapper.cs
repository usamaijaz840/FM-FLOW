using FMFlow.Integrations.Interface.DTOs;
using FMFlow.Integrations.Service.DTOs;
using Riok.Mapperly.Abstractions;

namespace FMFlow.Integrations.Service.Mapper;

[Mapper]
public partial class PlacesMapper
{
	[MapperIgnoreTarget(nameof(GooglePlacesAutocompleteRequest.LocationBias))]
	[MapperIgnoreTarget(nameof(GooglePlacesAutocompleteRequest.LocationRestriction))]
	[MapperIgnoreTarget(nameof(GooglePlacesAutocompleteRequest.Origin))]
	[MapperIgnoreTarget(nameof(GooglePlacesAutocompleteRequest.RegionCode))]
	[MapperIgnoreTarget(nameof(GooglePlacesAutocompleteRequest.IncludedPrimaryTypes))]
	[MapperIgnoreTarget(nameof(GooglePlacesAutocompleteRequest.IncludeQueryPredictions))]
	internal partial GooglePlacesAutocompleteRequest MapToGoogleRequest(AddressAutocompleteRequestDto request);

	internal AddressAutocompleteResponseDto MapToResponseDto(GooglePlacesAutocompleteResponse googleResponse)
	{
		var suggestions = googleResponse.Suggestions
			.Select(s => s.PlacePrediction)
			.Where(p => p != null)
			.Select(p => new AddressPredictionDto(
				PlaceId: p!.PlaceId,
				FullText: p.Text.Text,
				MainText: p.StructuredFormat.MainText.Text,
				SecondaryText: p.StructuredFormat.SecondaryText.Text
			))
			.ToList();

		return new AddressAutocompleteResponseDto(suggestions);
	}

	internal PlaceDetailsResponseDto MapToResponseDto(GooglePlaceDetailsResponse googleResponse)
	{
		var components = ExtractAddressComponents(googleResponse.AddressComponents);

		return new PlaceDetailsResponseDto(
			FormattedAddress: googleResponse.FormattedAddress,
			AddressComponents: components
		);
	}

	private static AddressComponentsDto ExtractAddressComponents(List<AddressComponent> components)
	{
		string? streetNumber = null;
		string? route = null;
		string? city = null;
		string? state = null;
		string? zipCode = null;

		foreach (var component in components)
		{
			if (component.Types.Contains("street_number"))
				streetNumber = component.LongText;
			else if (component.Types.Contains("route"))
				route = component.LongText;
			else if (component.Types.Contains("locality"))
				city = component.LongText;
			else if (component.Types.Contains("administrative_area_level_1"))
				state = component.ShortText;
			else if (component.Types.Contains("postal_code"))
				zipCode = component.LongText;
		}

		string streetAddress = string.Join(" ", new[] { streetNumber, route }.Where(s => !string.IsNullOrWhiteSpace(s)));

		return new AddressComponentsDto(
			AddressLine1: !string.IsNullOrWhiteSpace(streetAddress) ? streetAddress : null,
			City: city,
			State: state,
			ZipCode: zipCode
		);
	}
}
