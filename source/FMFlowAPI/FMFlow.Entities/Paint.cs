using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FMFlow.Entities.ModelInterfaces;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.Entities;

[Index(nameof(ProductLineId), IsUnique = true)]
public class Paint : IHasNameProperty, IHasDateChangeTracking, IHasDeleted
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int PaintId { get; set; }

	[ForeignKey(nameof(FlowUser))]
	public int? ProUserId { get; set; }

	public virtual FlowUser? ProUser { get; set; }

	public string Name { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public PaintAreaType? PaintAreaType { get; set; }

	[Required]
	public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;

	public DateTimeOffset? DateUpdated { get; set; }

	public bool IsDeleted { get; set; }

	public DateTimeOffset? DateDeleted { get; set; }

	public List<PaintSheen>? PaintSheens { get; set; }

	public string? ProductLineId { get; set; }

	public string? ProductCategory { get; set; }

	public string? Warranty { get; set; }

	public string? Substrate { get; set; }

	public string? SurfacePreparation { get; set; }

	public string? Cleanup { get; set; }

	public string? MarketingCopy { get; set; }

	public string? SherwinWilliamsPictureURL { get; set; }

	[ForeignKey(nameof(FileItem))]
	public int? PictureFileId { get; set; }
	public virtual FileItem? PictureFile { get; set; }

	[ForeignKey(nameof(ThumbnailFile))]
	public int? ThumbnailFileId { get; set; }
	public FileItem? ThumbnailFile { get; set; }

	public virtual List<Color>? Colors { get; set; }

	public TintCategory? TintCategory { get; set; }

	public string? GetPictureFileUrl()
	{
		if (PictureFileId == null)
			return null;

		return $"api/Paints/{PaintId}/Files/{PictureFileId}";
	}

	public string? GetThumbnailPictureFileUrl()
	{
		if (ThumbnailFileId == null)
			return null;

		return $"api/Paints/{PaintId}/Thumbnails/{ThumbnailFileId}";
	}
}

public enum PaintAreaType
{
	Interior,
	Exterior,
	InteriorAndExterior,
	Undefined
}
