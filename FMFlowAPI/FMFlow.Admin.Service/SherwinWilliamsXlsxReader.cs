using ClosedXML.Excel;

namespace FMFlow.Admin.Service;

public class SherwinWilliamsXlsxReader
{
	public static async Task<XlsxReaderResult> ReadAll(Stream stream)
	{
		using var wb = new XLWorkbook(stream);

		var fmFlow = ReadFMFlowSheet(wb.Worksheet("FM Flow"));
		var paintChips = ReadPaintChipsSheet(wb.Worksheet("Paint Chips"));
		var woodChips = ReadWoodChipsSheet(wb.Worksheet("Wood Chips"));

		return new XlsxReaderResult(fmFlow, paintChips, woodChips);
	}

	private static List<FMFlowItem> ReadFMFlowSheet(IXLWorksheet ws)
	{
		var (map, firstDataRow) = BuildHeaderMap(ws);

		var list = new List<FMFlowItem>();

		foreach (var row in ws.RowsUsed().Where(r => r.RowNumber() >= firstDataRow))
		{
			string g(string header) => Get(row, map, header);

			list.Add(new FMFlowItem(
				ProductLineId: g("Product Line ID"),
				ProductName: g("Product Name"),
				ProductCategory: g("Product Category"),
				Warranty: g("Warranty"),
				Substrate: g("Substrate"),
				SurfacePreparation: g("Surface Preparation"),
				Cleanup: g("Cleanup"),
				MarketingCopy: g("Marketing Copy"),
				FeatureBullets: [.. new List<string>
				{
					g("Feature Bullet 01"),
					g("Feature Bullet 02"),
					g("Feature Bullet 03"),
					g("Feature Bullet 04"),
					g("Feature Bullet 05"),
					g("Feature Bullet 06"),
					g("Feature Bullet 07"),
					g("Feature Bullet 08"),
					g("Feature Bullet 09"),
					g("Feature Bullet 10")
				}.Where(s => !string.IsNullOrEmpty(s))],
				Sheens: [.. new List<string>
				{
					g("Sheen 1"),
					g("Sheen 2"),
					g("Sheen 3"),
					g("Sheen 4"),
					g("Sheen 5"),
					g("Sheen 6")
				}.Where(s => !string.IsNullOrEmpty(s))],
				Colors: [.. new List<string>
				{
					g("Color 1"),
					g("Color 2"),
					g("Color 3"),
					g("Color 4"),
					g("Color 5"),
					g("Color 6"),
					g("Color 7"),
					g("Color 8"),
					g("Color 9"),
					g("Color 10"),
					g("Color 11"),
					g("Color 12"),
					g("Color 13"),
					g("Color 14"),
					g("Color 15")
				}.Where(s => !string.IsNullOrEmpty(s))],
				OriginalFormatPNG: g("Original Format PNG"),
				Reference: g("Reference")
			));


		}
		return list;
	}

	private static List<PaintChip> ReadPaintChipsSheet(IXLWorksheet ws)
	{
		var (map, firstDataRow) = BuildHeaderMap(ws);

		var list = new List<PaintChip>();

		foreach (var row in ws.RowsUsed().Where(r => r.RowNumber() >= firstDataRow))
		{
			string g(string header) => Get(row, map, header);

			list.Add(new PaintChip(
				Top50Interior: g("Top 50 Interior"),
				Top50Exterior: g("Top 50 Exterior"),
				FinesWhitesAndNeutrals: g("Finest Whites & Neutrals"),
				ColorNumber: g("Color Number"),
				ColorDisplayName: g("Color Display Name"),
				ColorType: g("Color Type"),
				ColorUsage: g("Color Usage"),
				ColorBrand: g("Color Brand"),
				ColorPrimaryFamily: g("Color Primary Family"),
				OriginalFormatPng: g("Original Format PNG")
			));
		}
		return list;
	}

	private static List<WoodChip> ReadWoodChipsSheet(IXLWorksheet ws)
	{
		var (map, firstDataRow) = BuildHeaderMap(ws);

		var list = new List<WoodChip>();

		foreach (var row in ws.RowsUsed().Where(r => r.RowNumber() >= firstDataRow))
		{
			string g(string header) => Get(row, map, header);

			list.Add(new WoodChip(

				MasterColorId: g("Master Color ID"),
				ColorName: g("Color Name"),
				ColorUsage: g("Color Usage"),
				ProductName: g("Product Name"),
				OriginalFormatPng: g("Original Format PNG")
			));
		}
		return list;
	}

	private static (Dictionary<string, int> map, int firstDataRow) BuildHeaderMap(IXLWorksheet ws)
	{
		var used = ws.RangeUsed() ?? throw new InvalidOperationException($"Empty sheet: {ws.Name}");
		var headerRow = used.FirstRowUsed().RowNumber();
		var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		foreach (var cell in ws.Row(headerRow).CellsUsed())
		{
			var name = (cell.GetString() ?? "").Trim();

			if (!string.IsNullOrEmpty(name))
				map[name] = cell.Address.ColumnNumber;
		}

		return (map, headerRow + 1);
	}

	private static string Get(IXLRow row, Dictionary<string, int> map, string header)
	{
		if (!map.TryGetValue(header, out var col))
			return "";

		var cell = row.Cell(col);

		return cell?.GetFormattedString().Trim() ?? "";
	}
}

public sealed record XlsxReaderResult(
	List<FMFlowItem> FMFlow,
	List<PaintChip> PaintChips,
	List<WoodChip> WoodChips);

public sealed record FMFlowItem(
	string ProductLineId,
	string ProductName,
	string ProductCategory,
	string? Warranty,
	string? Substrate,
	string? SurfacePreparation,
	string? Cleanup,
	string? MarketingCopy,
	string? OriginalFormatPNG,
	List<string>? FeatureBullets,
	List<string>? Sheens,
	List<string>? Colors,
	//string? Tintable, (ignored for the moment)
	string? Reference);

public sealed record PaintChip(
	string Top50Interior,
	string Top50Exterior,
	string FinesWhitesAndNeutrals,
	string ColorNumber,
	string ColorDisplayName,
	string ColorType,
	string ColorUsage,
	string ColorBrand,
	string ColorPrimaryFamily,
	string OriginalFormatPng);

public sealed record WoodChip(
	string MasterColorId,
	string ColorName,
	string ColorUsage,
	string ProductName,
	string OriginalFormatPng);
