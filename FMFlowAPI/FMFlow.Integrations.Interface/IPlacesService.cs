using FMFlow.FlowAPI.Interface;
using FMFlow.Integrations.Interface.DTOs;

namespace FMFlow.Integrations.Interface;

public interface IPlacesService
{
	Task<Result<AddressAutocompleteResponseDto>> GetAutocomplete(
		AddressAutocompleteRequestDto request,
		CancellationToken ct);

	Task<Result<PlaceDetailsResponseDto>> GetPlaceDetails(
		string placeId,
		string? sessionToken,
		CancellationToken ct);
}
