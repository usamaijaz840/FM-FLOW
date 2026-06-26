using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using FMFlow.FlowAPI.Interface;
using FMFlow.Integrations.Interface;
using FMFlow.Integrations.Interface.DTOs;
using Microsoft.Extensions.Logging;

namespace FMFlow.Integrations.Service;

/// <summary>
/// Mock address data structure
/// </summary>
internal class MockAddress
{
	public string PlaceId { get; set; } = string.Empty;
	public string Address { get; set; } = string.Empty;
	public string City { get; set; } = string.Empty;
	public string State { get; set; } = string.Empty;
	public string Zip { get; set; } = string.Empty;
}

/// <summary>
/// Mock implementation of IPlacesService for local development/testing
/// Returns fake data from CSV file without calling Google Places API
/// </summary>
public class MockPlacesService : IPlacesService
{
	private readonly List<MockAddress> _mockAddresses;
	private readonly ILogger<MockPlacesService> _logger;

	public MockPlacesService(ILogger<MockPlacesService> logger)
	{
		_logger = logger;
		_mockAddresses = LoadMockAddressesFromCsv();
		_logger.LogInformation("[MockPlacesService] Loaded {Count} mock addresses from CSV", _mockAddresses.Count);
	}

	private List<MockAddress> LoadMockAddressesFromCsv()
	{
		var csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "mock-addresses.csv");

		try
		{
			if (!File.Exists(csvPath))
			{
				_logger.LogWarning("[MockPlacesService] CSV file not found at {Path}. Using default addresses.", csvPath);
				return GetDefaultAddresses();
			}

			var config = new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				TrimOptions = TrimOptions.Trim,
				MissingFieldFound = null
			};

			using var reader = new StreamReader(csvPath);
			using var csv = new CsvReader(reader, config);

			var addresses = csv.GetRecords<MockAddress>().ToList();

			_logger.LogInformation("[MockPlacesService] Successfully loaded {Count} addresses from CSV", addresses.Count);
			return addresses.Count > 0 ? addresses : GetDefaultAddresses();
		}
		catch (IOException ex)
		{
			_logger.LogError(ex, "[MockPlacesService] IO ERROR loading CSV. Using default addresses.");
			return GetDefaultAddresses();
		}
		catch (UnauthorizedAccessException ex)
		{
			_logger.LogError(ex, "[MockPlacesService] ACCESS ERROR loading CSV. Using default addresses.");
			return GetDefaultAddresses();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "[MockPlacesService] ERROR loading CSV. Using default addresses.");
			return GetDefaultAddresses();
		}
	}

	private List<MockAddress> GetDefaultAddresses()
	{
		// Fallback addresses if CSV fails to load
		return new List<MockAddress>
		{
			new() { PlaceId = "mock_place_1", Address = "123 Main St", City = "San Francisco", State = "CA", Zip = "94102" },
			new() { PlaceId = "mock_place_2", Address = "456 Oak Ave", City = "Los Angeles", State = "CA", Zip = "90001" },
			new() { PlaceId = "mock_place_3", Address = "789 Pine Rd", City = "San Diego", State = "CA", Zip = "92101" },
			new() { PlaceId = "mock_place_4", Address = "321 Elm Blvd", City = "Sacramento", State = "CA", Zip = "95814" },
			new() { PlaceId = "mock_place_5", Address = "654 Maple Dr", City = "San Jose", State = "CA", Zip = "95113" },
		};
	}

	public Task<Result<AddressAutocompleteResponseDto>> GetAutocomplete(
		AddressAutocompleteRequestDto request,
		CancellationToken ct)
	{
		_logger.LogInformation("[MockPlacesService] Autocomplete request for: '{Input}'", request.Input);

		if (string.IsNullOrWhiteSpace(request.Input))
		{
			return Task.FromResult(Result<AddressAutocompleteResponseDto>.Failure(
				"Input is required",
				ResultErrorType.BadRequest
			));
		}

		// Filter mock addresses based on input
		var searchLower = request.Input.ToLower();
		var matches = _mockAddresses
			.Where(a =>
				a.Address.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ||
				a.City.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ||
				a.State.Contains(searchLower, StringComparison.OrdinalIgnoreCase))
			.OrderByDescending(a => a.Address.StartsWith(searchLower, StringComparison.OrdinalIgnoreCase))
			.ThenBy(a => a.Address)
			.Take(5)
			.Select(a => new AddressPredictionDto(
				PlaceId: a.PlaceId,
				FullText: $"{a.Address}, {a.City}, {a.State} {a.Zip}, USA",
				MainText: a.Address,
				SecondaryText: $"{a.City}, {a.State} {a.Zip}, USA"
			))
			.ToList();
		
		var response = new AddressAutocompleteResponseDto(matches);
		
		_logger.LogInformation("[MockPlacesService] Returning {Count} mock suggestions", matches.Count);

		return Task.FromResult(Result<AddressAutocompleteResponseDto>.Success(response));
	}

	public Task<Result<PlaceDetailsResponseDto>> GetPlaceDetails(
		string placeId,
		string? sessionToken,
		CancellationToken ct)
	{
		_logger.LogInformation("[MockPlacesService] Place details request for: '{PlaceId}', SessionToken: '{SessionToken}'", 
			placeId, sessionToken ?? "none");

		if (string.IsNullOrWhiteSpace(placeId))
		{
			return Task.FromResult(Result<PlaceDetailsResponseDto>.Failure(
				"PlaceId is required",
				ResultErrorType.BadRequest
			));
		}

	// Find mock address by place ID
	var mockAddress = _mockAddresses.FirstOrDefault(a => a.PlaceId == placeId);

	if (mockAddress == default)
		{
			return Task.FromResult(Result<PlaceDetailsResponseDto>.Failure(
				"Place not found",
				ResultErrorType.NotFound
			));
		}

		var response = new PlaceDetailsResponseDto(
			FormattedAddress: $"{mockAddress.Address}, {mockAddress.City}, {mockAddress.State} {mockAddress.Zip}, USA",
			AddressComponents: new AddressComponentsDto(
				AddressLine1: mockAddress.Address,
				City: mockAddress.City,
				State: mockAddress.State,
				ZipCode: mockAddress.Zip
			)
		);

		_logger.LogInformation("[MockPlacesService] Returning mock place details for: {Address}", mockAddress.Address);

		return Task.FromResult(Result<PlaceDetailsResponseDto>.Success(response));
	}
}
