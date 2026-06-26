using System.Text;
using EFRepository;
using FMFlow.Admin.Interface;
using FMFlow.Entities;
using FMFlow.Files.Interface;
using FMFlow.Files.Interface.DTOs;
using FMFlow.FlowAPI.Interface;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.Admin.Service;

public class PaintsIngestor(IRepository repository, IFilesService filesService) : IPaintsIngestor
{
	public async Task<Result> ProcessXlsx(Stream xlsxStream, CancellationToken ct)
	{
		XlsxReaderResult fileResult;

		try
		{
			fileResult = await SherwinWilliamsXlsxReader.ReadAll(xlsxStream);
		}
		catch (Exception ex)
		{
			return Result.Failure($"Error when reading Xlsx file. Type {ex}. Message: {ex.Message}.");
		}

		await ProcessPaints(fileResult.FMFlow, ct);
		await ProcessPaintChipColors(fileResult.PaintChips, ct);
		await ProcessWoodChipsColors(fileResult.WoodChips, ct);

		return Result.Success();
	}

	private async Task ProcessPaints(List<FMFlowItem> items, CancellationToken ct)
	{
		foreach (var item in items)
		{
			ct.ThrowIfCancellationRequested();

			if (item == null)
				continue;

			Paint? paint;

			paint = await repository
				.Query<Paint>()
				.Include(p => p.Colors)
				.FirstOrDefaultAsync(p => p.ProductLineId == item.ProductLineId, ct);

			Result<ImageUploadResultDto>? uploadResult = null;

			// If there's an image URL and it's different from the existing paint's image, update it:
			if (item.OriginalFormatPNG != null &&
				(paint == null || paint.SherwinWilliamsPictureURL != item.OriginalFormatPNG))
			{
				if (paint?.PictureFileId.HasValue == true)
					await filesService.DeleteFile(paint.PictureFileId.Value, ct);

				var fileName = $"{item.ProductLineId}_{item.ProductName}.png";
				uploadResult = await DownloadAndUploadImage(item.OriginalFormatPNG, fileName, ct);
			}

			paint ??= new Paint();

			paint.ProductLineId = item.ProductLineId;
			paint.Name = item.ProductName;
			paint.ProductCategory = item.ProductCategory;
			paint.Warranty = item.Warranty;
			paint.Substrate = item.Substrate;
			paint.SurfacePreparation = item.SurfacePreparation;
			paint.Cleanup = item.Cleanup;
			paint.MarketingCopy = FormatMarketingContent(item);
			paint.SherwinWilliamsPictureURL = item.OriginalFormatPNG;
			paint.TintCategory = GetTintCategory(item.Reference);
			paint.PaintAreaType = GetPaintAreaType(item.ProductName);

			if (uploadResult != null && uploadResult.Value != null && uploadResult.IsSuccess)
			{
				paint.PictureFileId = uploadResult.Value.FileId;
				paint.ThumbnailFileId = uploadResult.Value.ThumbnailFileId;
			}

			await ProcessProductSheens(item, paint, ct);
			await ProcessPaintColors(item, paint);

			repository.AddOrUpdate(paint);
			await repository.SaveAsync(ct);
		}
	}

	private async Task ProcessProductSheens(FMFlowItem item, Paint paint, CancellationToken ct)
	{
		var sheens = new List<string?>();

		if (item.Sheens != null && item.Sheens.Count != 0)
			sheens.AddRange(item.Sheens.Where(s => !string.IsNullOrEmpty(s)));

		foreach (var fileSheen in sheens)
		{
			var sheenExists = await repository
				.Query<PaintSheen>()
				.ByPaintId(paint.PaintId)
				.AnyAsync(ps => ps.Sheen.Name == fileSheen, ct);

			if (sheenExists)
				continue;

			Sheen? sheen;

			sheen = await repository
				.Query<Sheen>()
				.FirstOrDefaultAsync(s => s.Name == fileSheen, ct);

			sheen ??= new Sheen { Name = fileSheen! };

			repository.AddOrUpdate(new PaintSheen
			{
				Paint = paint,
				Sheen = sheen
			});
		}

		// Delete PaintSheens that are not in the new list
		if (paint.PaintId != 0) // Only for existing paints
		{
			var existingSheens = await repository
				.Query<PaintSheen>()
				.ByPaintId(paint.PaintId)
				.Where(ps => !ps.IsDeleted)
				.Include(ps => ps.Sheen)
				.ToListAsync(ct);

			var sheenNamesToKeep = sheens.Where(s => !string.IsNullOrEmpty(s)).ToList();

			foreach (var paintSheen in existingSheens)
			{
				if (!sheenNamesToKeep.Contains(paintSheen.Sheen.Name))
				{
					paintSheen.IsDeleted = true;
					paintSheen.DateDeleted = DateTimeOffset.UtcNow;
				}
			}
		}
	}

	private static async Task ProcessPaintColors(FMFlowItem item, Paint paint)
	{
		paint.Colors ??= [];

		if (item.Colors != null)
		{
			foreach (var colorName in item.Colors)
			{
				bool colorExists = paint.Colors.Any(c => !c.IsDeleted && c.Name == colorName);

				if (!colorExists) // otherwise update tint color and dateupdated
				{
					paint.Colors.Add(new Color
					{
						Name = colorName,
						TintCategory = GetTintCategory(item.Reference) // This returns always null so far because it seems Paints have either
																	   // colors or reference, not both
					});
				}
			}

			// Mark colors for deletion if they're not in the new list
			foreach (var existingColor in paint.Colors.Where(c => !c.IsDeleted))
				if (!item.Colors.Contains(existingColor.Name))
					(existingColor.IsDeleted, existingColor.DateDeleted) = (true, DateTimeOffset.UtcNow);
		}
		else if (paint.Colors.Any(c => !c.IsDeleted))
		{
			// Mark all existing colors as deleted if no colors are provided
			foreach (var existingColor in paint.Colors.Where(c => !c.IsDeleted))
			{
				existingColor.IsDeleted = true;
				existingColor.DateDeleted = DateTimeOffset.UtcNow;
			}
		}
	}

	private static string FormatMarketingContent(FMFlowItem item)
	{
		if (item == null)
			return string.Empty;

		var result = new StringBuilder();

		if (!string.IsNullOrEmpty(item.MarketingCopy))
		{
			var paragraphs = item.MarketingCopy
				.Split(["\r\n", "\r", "\n"], StringSplitOptions.None)
				.Where(p => !string.IsNullOrWhiteSpace(p))
				.Select(p => $"<p>{p.Trim()}</p>");

			foreach (var paragraph in paragraphs)
				result.AppendLine(paragraph);
		}

		var bullets = new List<string>();

		if (item.FeatureBullets != null && item.FeatureBullets.Count != 0)
			foreach (var featureBullet in item.FeatureBullets)
			{
				if (!string.IsNullOrWhiteSpace(featureBullet))
					bullets.Add(featureBullet);
			}

		if (bullets.Count != 0)
		{
			result.AppendLine("<ul>");

			foreach (var bullet in bullets)
				result.AppendLine($"<li>{bullet.Trim()}</li>");

			result.AppendLine("</ul>");
		}

		return result.ToString().Trim();
	}

	private async Task<Result<ImageUploadResultDto>> DownloadAndUploadImage(string imageUrl, string fileName, CancellationToken ct)
	{
		try
		{
			byte[] imageBytes;

			// Download from external URL
			using var httpClient = new HttpClient();
			imageBytes = await httpClient.GetByteArrayAsync(imageUrl, ct);

			if (imageBytes == null || imageBytes.Length == 0)
				return null;

			// Determine content type based on file extension
			var extension = Path.GetExtension(fileName).ToLowerInvariant();

			var contentType = extension switch
			{
				".png" => "image/png",
				".jpg" or ".jpeg" => "image/jpeg",
				".gif" => "image/gif",
				_ => "application/octet-stream"
			};

			var fileUploadRequest = new FileUploadRequestDto
			{
				FileBytes = imageBytes,
				FileName = fileName,
				ContentType = contentType
			};

			return await filesService.UploadImageAsync(fileUploadRequest, ct);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error downloading/uploading image {imageUrl}: {ex.Message}");

			return Result<ImageUploadResultDto>.Failure(ex.Message);
		}
	}

	private async Task ProcessPaintChipColors(List<PaintChip> paintChips, CancellationToken ct)
	{
		foreach (var chip in paintChips)
		{
			ct.ThrowIfCancellationRequested();

			if (chip == null)
				continue;

			Color? color = await repository
				.Query<Color>()
				.FirstOrDefaultAsync(c => c.SWColorId == chip.ColorNumber, ct);

			bool isNew = color == null;
			bool needsUpdate = false;

			if (!isNew)
				needsUpdate = color!.Name != chip.ColorDisplayName || color!.SherwinWilliamsPictureURL != chip.OriginalFormatPng;

			Result<ImageUploadResultDto>? uploadResult = null;

			if (!string.IsNullOrEmpty(chip.OriginalFormatPng) &&
				(color == null || color.SherwinWilliamsPictureURL != chip.OriginalFormatPng))
			{
				if (color?.PictureFileId.HasValue == true)
					await filesService.DeleteFile(color.PictureFileId.Value, ct);

				var fileName = $"{chip.ColorNumber}_{chip.ColorDisplayName}.png";
				uploadResult = await DownloadAndUploadImage(chip.OriginalFormatPng, fileName, ct);
			}

			color ??= new Color();

			color.SWColorId = chip.ColorNumber;
			color.Name = chip.ColorDisplayName;
			color.Top50Exterior = int.TryParse(chip.Top50Exterior, out var top50ext) ? top50ext : null;
			color.Top50Interior = int.TryParse(chip.Top50Interior, out var top50int) ? top50int : null;
			color.FinesWhitesAndNeutrals = chip.FinesWhitesAndNeutrals == "Y";
			color.PaintAreaType = GetPaintAreaType(chip.ColorUsage);
			color.Brand = chip.ColorBrand;
			color.PrimaryFamily = chip.ColorPrimaryFamily;
			color.SherwinWilliamsPictureURL = chip.OriginalFormatPng;
			color.TintCategory = TintCategory.PaintChip;

			if (uploadResult != null && uploadResult.Value != null && uploadResult.IsSuccess)
			{
				color.PictureFileId = uploadResult.Value.FileId;
				// thumbnail is not used for colors, and colors might be converted to HEX codes later
			}

			if (isNew || needsUpdate)
				color.DateUpdated = DateTimeOffset.UtcNow;

			repository.AddOrUpdate(color);
			await repository.SaveAsync(ct);
		}
	}

	private async Task ProcessWoodChipsColors(List<WoodChip> woodChips, CancellationToken ct)
	{
		foreach (var chip in woodChips)
		{
			ct.ThrowIfCancellationRequested();

			if (chip == null)
				continue;

			Color? color = await repository
				.Query<Color>()
				.FirstOrDefaultAsync(c => c.SWColorId == chip.MasterColorId, ct);

			bool isNew = color == null;
			bool needsUpdate = false;

			if (!isNew)
				needsUpdate =
					color!.Name != chip.ColorName ||
					color!.SherwinWilliamsPictureURL != chip.OriginalFormatPng ||
					color!.TintCategory != GetTintCategory(chip.ProductName);

			Result<ImageUploadResultDto>? uploadResult = null;

			if (!string.IsNullOrEmpty(chip.OriginalFormatPng) &&
				(color == null || color.SherwinWilliamsPictureURL != chip.OriginalFormatPng))
			{
				if (color?.PictureFileId.HasValue == true)
					await filesService.DeleteFile(color.PictureFileId.Value, ct);

				var fileName = $"{chip.MasterColorId}_{chip.ColorName}.png";
				uploadResult = await DownloadAndUploadImage(chip.OriginalFormatPng, fileName, ct);
			}

			color ??= new Color();

			color.SWColorId = chip.MasterColorId;
			color.Name = chip.ColorName;
			color.PaintAreaType = GetPaintAreaType(chip.ColorUsage);
			color.TintCategory = GetTintCategory(chip.ProductName);
			color.SherwinWilliamsPictureURL = chip.OriginalFormatPng;

			if (uploadResult != null && uploadResult.Value != null && uploadResult.IsSuccess)
			{
				color.PictureFileId = uploadResult.Value.FileId;
				// thumbnail is not used for colors, and colors might be converted to HEX codes later
			}

			if (isNew || needsUpdate)
				color.DateUpdated = DateTimeOffset.UtcNow;

			repository.AddOrUpdate(color);
			await repository.SaveAsync(ct);
		}
	}

	private static TintCategory? GetTintCategory(string? name) =>
		name switch
		{
			// values found in FMFlow tab:
			"Paint Chips" => TintCategory.PaintChip,
			"Wood Chips - Semi-Transparent" => TintCategory.WoodChipSemiTransparent,
			"Wood Chips - Solid" => TintCategory.WoodChipSolid,
			"Wood Chips - Deck & Dock" => TintCategory.WoodChipDeckAndDock,
			// values found in Wood Chips tab:
			"Semi-Transparent" => TintCategory.WoodChipSemiTransparent,
			"Solid" => TintCategory.WoodChipSolid,
			"Deck & Dock" => TintCategory.WoodChipDeckAndDock,
			_ => null
		};

	private static PaintAreaType GetPaintAreaType(string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return PaintAreaType.Undefined;

		text = text.ToLowerInvariant();

		bool hasInterior = text.Contains("interior");
		bool hasExterior = text.Contains("exterior");

		if (hasInterior && hasExterior)
			return PaintAreaType.InteriorAndExterior;
		else if (hasExterior)
			return PaintAreaType.Exterior;
		else if (hasInterior)
			return PaintAreaType.Interior;
		else
			return PaintAreaType.Undefined;
	}

}

