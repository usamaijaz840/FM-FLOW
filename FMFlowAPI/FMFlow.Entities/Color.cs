using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.Entities;

[Index(nameof(SWColorId), IsUnique = true)]
public class Color
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int ColorId { get; set; }

	public string? SWColorId { get; set; }

	[Required]
	public string Name { get; set; } = null!;

	public string? ColorType { get; set; }

	public string? Brand { get; set; }

	public string? PrimaryFamily { get; set; }

	public TintCategory? TintCategory { get; set; }

	public int? Top50Interior { get; set; }

	public int? Top50Exterior { get; set; }

	public PaintAreaType? PaintAreaType { get; set; }

	public bool? FinesWhitesAndNeutrals { get; set; }

	public string? SherwinWilliamsPictureURL { get; set; }

	public virtual FileItem? PictureFile { get; set; }

	[ForeignKey(nameof(FileItem))]
	public int? PictureFileId { get; set; }

	public virtual Paint? Paint { get; set; }

	[ForeignKey(nameof(Paint))]
	public int? PaintId { get; set; }

	public bool IsDeleted { get; set; }

	public DateTimeOffset? DateDeleted { get; set; }

	public DateTimeOffset DateUpdated { get; set; }

	public string? GetPictureFileUrl()
	{
		if (PictureFileId == null)
			return null;

		return $"api/Colors/{ColorId}/Files/{PictureFileId}";
	}
}

public enum TintCategory
{
	PaintChip,
	WoodChipSemiTransparent,
	WoodChipSolid,
	WoodChipDeckAndDock
}
