using System.Text.Json;
using EFRepository;
using FMFlow.Entities;
using FMFlow.Estimates.Interface;
using FMFlow.Estimates.Interface.Models;

namespace FMFlow.Estimates.Service;

public class EstimateCalculatorService(IRepository repository) : IEstimateCalculatorService
{
	decimal sundriesSubTotal = 0m;
	decimal estimateSubTotal = 0m;
	bool baseboardFoundOnInterior = false;
	int proUserId;

	public EstimateCalculationResponse Calculate(JsonDocument attributes, int proUserId)
	{
		this.proUserId = proUserId;
		var results = new Dictionary<string, object>();
		var root = attributes.RootElement;

		if (root.TryGetProperty("exteriorAreas", out var exteriors))
			foreach (var exterior in exteriors.EnumerateArray())
			{
				var id = exterior.GetProperty("id").GetString()!;
				var exteriorResults = CalculateExteriorPaintingTotal(exterior);

				results[id] = new
				{
					exteriorTotal = exteriorResults
				};
			}

		if (root.TryGetProperty("interiorAreas", out var interiors))
			foreach (var interior in interiors.EnumerateArray())
			{
				var id = interior.GetProperty("id").GetString()!;
				decimal ceilingTotal = 0;
				var trimMoldingAreasTotal = new Dictionary<string, decimal>();

				if (interior.TryGetProperty("ceilingArea", out var ceiling))
				{
					var ceilingResults = CalculateCeilingTotal(ceiling, interior);
					ceilingTotal = ceilingResults;
				}

				if (interior.TryGetProperty("trimMoldingAreas", out var interiorTrimMoldingAreas))
					foreach (var trimMoldingArea in interiorTrimMoldingAreas.EnumerateArray())
					{
						string moldingItem = trimMoldingArea.GetProperty("moldingItem").GetString()!;
						decimal trimMoldingResults = CalculateTrimMoldingTotal(trimMoldingArea, true);
						trimMoldingAreasTotal[moldingItem] = trimMoldingResults;
					}

				var interiorResults = CalculateInteriorPaintingTotal(interior);

				results[id] = new
				{
					interiorTotal = interiorResults,
					ceilingTotal,
					trimMoldingAreas = trimMoldingAreasTotal
				};
			}

		if (root.TryGetProperty("ceilingAreas", out var ceilingAreas))
			foreach (var ceilingArea in ceilingAreas.EnumerateArray())
			{
				var id = ceilingArea.GetProperty("id").GetString()!;
				var ceilingResults = CalculateCeilingTotal(ceilingArea);

				results[id] = new { ceilingTotal = ceilingResults };
			}

		if (root.TryGetProperty("otherItems", out var otherItems))
			foreach (var item in otherItems.EnumerateArray())
			{
				var id = item.GetProperty("id").GetString()!;
				var amount = item.GetProperty("amount").GetDecimal();
				var price = item.GetProperty("price").GetDecimal();
				var itemTotal = amount * price;

				estimateSubTotal += itemTotal;

				results[id] = new { itemTotal };
			}

		if (root.TryGetProperty("trimMoldingAreas", out var trimMoldingAreas))
			foreach (var trimMoldingArea in trimMoldingAreas.EnumerateArray())
			{
				var id = trimMoldingArea.GetProperty("id").GetString()!;
				var trimMoldingTotal = CalculateTrimMoldingTotal(trimMoldingArea);

				results[id] = new { trimMoldingTotal };
			}

		var estimateTotalBeforeDiscount = sundriesSubTotal + estimateSubTotal;

		string? discountType = null;
		decimal discountInput = 0m;
		decimal discountAmount = 0m;

		if (root.TryGetProperty("discountType", out var discountTypeProp) &&
			discountTypeProp.ValueKind == JsonValueKind.String)
		{
			discountType = discountTypeProp.GetString();
		}

		if (root.TryGetProperty("discount", out var discountProp) &&
			discountProp.ValueKind == JsonValueKind.Number)
		{
			discountInput = discountProp.GetDecimal();
		}

		if (!string.IsNullOrWhiteSpace(discountType) && discountInput > 0)
		{
			switch (discountType.Trim().ToLower())
			{
				case "percentage":
					var percentage = discountInput / 100m;
					discountAmount = Math.Round(estimateTotalBeforeDiscount * percentage, 2, MidpointRounding.AwayFromZero);
					break;

				case "flatamount":
					discountAmount = Math.Round(discountInput, 2, MidpointRounding.AwayFromZero);
					break;

				default:
					throw new ArgumentException("Invalid discount type");
			}

			// Cap discount so total never goes negative
			if (discountAmount > estimateTotalBeforeDiscount)
				discountAmount = estimateTotalBeforeDiscount;
		}

		results["subtotals"] = new
		{
			sundriesSubTotal,
			estimateSubTotal,
			estimateTotalBeforeDiscount,
			discount = discountAmount
		};

		var json = JsonSerializer.Serialize(results);
		var calculationResultsJson = JsonDocument.Parse(json);

		return new EstimateCalculationResponse
		{
			CalculationResultsJson = calculationResultsJson,
			Total = estimateTotalBeforeDiscount - discountAmount
		};
	}

	private decimal CalculateExteriorPaintingTotal(JsonElement wall)
	{
		decimal length = wall.GetProperty("length").GetDecimal();
		decimal height = wall.GetProperty("height").GetDecimal();
		int coats = wall.GetProperty("coats").GetInt32();
		int paintSheenId = wall.GetProperty("paintSheenId").GetInt32();
		decimal multiplier = wall.GetProperty("materialMultiplierPrice").GetDecimal();
		string coatingThickness = wall.GetProperty("coatingThickness").GetString()!;

		decimal area = length * height;
		decimal coverage = GetCoverageConstant(coatingThickness);
		decimal gallons = area * coats / coverage;
		decimal gallonsPurchased = Math.Ceiling(gallons);

		var pricePerGallon = repository.Query<PaintSheenPrice>()
			.Where(p => p.PaintSheenId == paintSheenId && p.ProUserId == proUserId)
			.Select(p => p.PricePerGallon)
			.First();

		decimal exactPaintCost = Math.Round(gallonsPurchased * pricePerGallon, 2);

		decimal sundries = Math.Round(exactPaintCost * 0.20m, 2);

		var sundriesWithMultiplierTotal = sundries * multiplier;
		sundriesSubTotal += sundriesWithMultiplierTotal;

		var exactPaintCostWithMultiplierTotal = exactPaintCost * multiplier;
		estimateSubTotal += exactPaintCostWithMultiplierTotal;

		var total = (sundries + exactPaintCost) * multiplier;

		return total;
	}

	private decimal CalculateInteriorPaintingTotal(JsonElement wall)
	{
		decimal length = wall.GetProperty("length").GetDecimal();
		decimal width = wall.GetProperty("width").GetDecimal();
		decimal height = wall.GetProperty("height").GetDecimal();
		int coats = wall.GetProperty("coats").GetInt32();
		int paintSheenId = wall.GetProperty("paintSheenId").GetInt32();
		decimal multiplier = wall.GetProperty("materialMultiplierPrice").GetDecimal();
		string coatingThickness = wall.GetProperty("coatingThickness").GetString()!;

		decimal area = length * height * 2 + width * height * 2;
		decimal coverage = GetCoverageConstant(coatingThickness);
		decimal gallons = area * coats / coverage;
		decimal gallonsPurchased = Math.Ceiling(gallons);

		var pricePerGallon = repository.Query<PaintSheenPrice>()
			.Where(p => p.PaintSheenId == paintSheenId && p.ProUserId == proUserId)
			.Select(p => p.PricePerGallon)
			.First();

		decimal exactPaintCost = Math.Round(gallonsPurchased * pricePerGallon, 2);

		decimal sundries = Math.Round(exactPaintCost * 0.20m, 2);

		var sundriesWithMultiplierTotal = sundries * multiplier;
		sundriesSubTotal += sundriesWithMultiplierTotal;

		var exactPaintCostWithMultiplierTotal = exactPaintCost * multiplier;
		estimateSubTotal += exactPaintCostWithMultiplierTotal;

		var total = (sundries + exactPaintCost) * multiplier;

		return total;
	}

	private decimal CalculateCeilingTotal(JsonElement ceiling, JsonElement? parent = null)
	{
		decimal length = parent?.GetProperty("length").GetDecimal() ?? ceiling.GetProperty("length").GetDecimal();
		decimal width = parent?.GetProperty("width").GetDecimal() ?? ceiling.GetProperty("width").GetDecimal();
		int coats = ceiling.GetProperty("coats").GetInt32();
		int paintSheenId = ceiling.GetProperty("paintSheenId").GetInt32();
		string thickness = ceiling.GetProperty("coatingThickness").GetString()!;
		decimal multiplier = ceiling.GetProperty("materialMultiplierPrice").GetDecimal();

		decimal area = length * width;
		decimal coverage = GetCoverageConstant(thickness);
		decimal gallons = area * coats / coverage;
		decimal gallonsPurchased = Math.Ceiling(gallons);

		var pricePerGallon = repository.Query<PaintSheenPrice>()
			.Where(p => p.PaintSheenId == paintSheenId && p.ProUserId == proUserId)
			.Select(p => p.PricePerGallon)
			.First();

		decimal exactPaintCost = Math.Round(gallonsPurchased * pricePerGallon, 2);

		decimal sundries = Math.Round(exactPaintCost * 0.20m, 2);

		var sundriesWithMultiplierTotal = sundries * multiplier;
		sundriesSubTotal += sundriesWithMultiplierTotal;

		var exactPaintCostWithMultiplierTotal = exactPaintCost * multiplier;
		estimateSubTotal += exactPaintCostWithMultiplierTotal;

		var total = (sundries + exactPaintCost) * multiplier;

		return total;
	}

	private decimal CalculateTrimMoldingTotal(JsonElement trimMoldingArea, bool forInteriorArea = false)
	{
		var length = trimMoldingArea.GetProperty("length").GetDecimal();
		var width = trimMoldingArea.GetProperty("width").GetDecimal();
		var coats = trimMoldingArea.GetProperty("coats").GetInt32();
		string moldingItem = trimMoldingArea.GetProperty("moldingItem").GetString()!;
		var moldingItemConstant = GetMoldingItemConstant(moldingItem);

		var trimMoldingAreaTotal = (length * 2 + width * 2) * moldingItemConstant * coats;
		estimateSubTotal += trimMoldingAreaTotal;

		if (forInteriorArea && moldingItem.Equals("baseboard", StringComparison.CurrentCultureIgnoreCase))
			baseboardFoundOnInterior = true;

		return trimMoldingAreaTotal;
	}

	private static decimal GetCoverageConstant(string thickness)
	{
		return thickness.ToLower() switch
		{
			"light" => 350m,
			"medium" => 275m,
			"heavy" => 200m,
			_ => throw new ArgumentException("Invalid coating thickness")
		};
	}

	private static decimal GetMoldingItemConstant(string moldingItem)
	{
		return moldingItem.ToLower() switch
		{
			"baseboard" => 2.0m,
			"crown" => 2.5m,
			"chair" => 0.75m,
			_ => throw new ArgumentException("Invalid molding item")
		};
	}
}
