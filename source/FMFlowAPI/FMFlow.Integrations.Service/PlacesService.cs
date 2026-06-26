using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FMFlow.FlowAPI.Interface;
using FMFlow.Integrations.Interface;
using FMFlow.Integrations.Interface.DTOs;
using FMFlow.Integrations.Service.DTOs;
using FMFlow.Integrations.Service.Mapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FMFlow.Integrations.Service;

public class PlacesService(
	HttpClient httpClient,
	IOptions<GoogleSettings> settings,
	PlacesMapper mapper,
	ILogger<PlacesService> logger) : IPlacesService
{
	private readonly GoogleSettings _settings = settings.Value;

	private const string GENERIC_ERROR_MESSAGE = "Unable to retrieve address details at this time. Please try again.";

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		WriteIndented = false
	};

	public async Task<Result<AddressAutocompleteResponseDto>> GetAutocomplete(
		AddressAutocompleteRequestDto request,
		CancellationToken ct)
	{
		try
		{
			// Validate input
			if (string.IsNullOrWhiteSpace(request.Input))
			{
				return Result<AddressAutocompleteResponseDto>.Failure(
					"Input is required",
					ResultErrorType.BadRequest
				);
			}

			// Map DTO to Google API request format
			var googleRequest = mapper.MapToGoogleRequest(request);

			// Prepare HTTP request
			string url = $"{_settings.PlacesApiUrl}/places:autocomplete";
			var content = new StringContent(
				JsonSerializer.Serialize(googleRequest, JsonOptions),
				Encoding.UTF8,
				"application/json"
			);

			using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
			{
				Content = content
			};

			// Add required headers
			httpRequest.Headers.Add("X-Goog-Api-Key", _settings.PlacesApiKey);

			// Field mask to specify which fields to return
			httpRequest.Headers.Add("X-Goog-FieldMask",
				"suggestions.placePrediction.placeId," +
				"suggestions.placePrediction.text," +
				"suggestions.placePrediction.structuredFormat"
			);

			// Send request to Google
			HttpResponseMessage response = await httpClient.SendAsync(httpRequest, ct);

			if (!response.IsSuccessStatusCode)
			{
				string errorContent = await response.Content.ReadAsStringAsync(ct);

				logger.LogError("Google Places API autocomplete error: {StatusCode}. {ErrorContent}",
					response.StatusCode, errorContent);

				return Result<AddressAutocompleteResponseDto>.Failure(
					GENERIC_ERROR_MESSAGE,
					ResultErrorType.BadRequest
				);
			}

			// Parse response
			string json = await response.Content.ReadAsStringAsync(ct);
			var googleResponse = JsonSerializer.Deserialize<GooglePlacesAutocompleteResponse>(
				json,
				JsonOptions
			);

			if (googleResponse == null)
			{
				logger.LogError("Failed to deserialize Google Places API autocomplete response: {Json}", json);

				return Result<AddressAutocompleteResponseDto>.Failure(
					GENERIC_ERROR_MESSAGE,
					ResultErrorType.BadRequest
				);
			}

			// Map Google response to our DTO
			AddressAutocompleteResponseDto mappedResponse = mapper.MapToResponseDto(googleResponse);

			return Result<AddressAutocompleteResponseDto>.Success(mappedResponse);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error in GetAutocomplete: {Error}", ex.Message);

			return Result<AddressAutocompleteResponseDto>.Failure(
				GENERIC_ERROR_MESSAGE,
				ResultErrorType.BadRequest
			);
		}
	}
	public async Task<Result<PlaceDetailsResponseDto>> GetPlaceDetails(
			string placeId,
			string? sessionToken,
			CancellationToken ct)
	{
		try
		{
			// Validate input
			if (string.IsNullOrWhiteSpace(placeId))
			{
				return Result<PlaceDetailsResponseDto>.Failure(
					"PlaceId is required",
					ResultErrorType.BadRequest
			);
			}

			// Prepare HTTP request with optional session token
			string url = string.IsNullOrWhiteSpace(sessionToken) 
				? $"places/{placeId}"
				: $"places/{placeId}?sessionToken={Uri.EscapeDataString(sessionToken)}";

			url = $"{_settings.PlacesApiUrl}/{url}";

			using var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);            // Add required headers
			httpRequest.Headers.Add("X-Goog-Api-Key", _settings.PlacesApiKey);

			// Field mask to specify which fields to return
			httpRequest.Headers.Add("X-Goog-FieldMask",
				"formattedAddress,addressComponents"
			);

			// Send request to Google
			var response = await httpClient.SendAsync(httpRequest, ct); if (!response.IsSuccessStatusCode)
			{
				string errorContent = await response.Content.ReadAsStringAsync(ct);

				logger.LogError("Google Places API place details error: {StatusCode}. {ErrorContent}",
					response.StatusCode, errorContent);

				return Result<PlaceDetailsResponseDto>.Failure(
					GENERIC_ERROR_MESSAGE,
					ResultErrorType.BadRequest
				);
			}

			// Parse response
			string json = await response.Content.ReadAsStringAsync(ct);
			var googleResponse = JsonSerializer.Deserialize<GooglePlaceDetailsResponse>(
				json,
				JsonOptions
			);

			if (googleResponse == null)
			{
				logger.LogError("Failed to deserialize Google Place Details API response: {Json}", json);

				return Result<PlaceDetailsResponseDto>.Failure(
					GENERIC_ERROR_MESSAGE,
					ResultErrorType.BadRequest
				);
			}

			// Map Google response to our DTO
			var mappedResponse = mapper.MapToResponseDto(googleResponse);

			return Result<PlaceDetailsResponseDto>.Success(mappedResponse);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error in GetPlaceDetails: {Error}", ex.Message);

			return Result<PlaceDetailsResponseDto>.Failure(
				GENERIC_ERROR_MESSAGE,
				ResultErrorType.BadRequest
			);
		}
	}
}
