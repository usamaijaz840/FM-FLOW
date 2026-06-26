using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FMFlow.Entities.ModelInterfaces;

namespace FMFlow.Entities;

public class FileItemToEstimate : IHasDateChangeTracking, IHasDeleted
{
	[ForeignKey(nameof(File))]
	public int FileID { get; set; }
	public FileItem File { get; set; }

	[ForeignKey(nameof(ThumbnailFile))]
	public int? ThumbnailFileID { get; set; }
	public FileItem? ThumbnailFile { get; set; }

	[ForeignKey(nameof(Estimate))]
	public int EstimateID { get; set; }
	public Estimate Estimate { get; set; }

	public EstimateFileType FileType { get; set; } = EstimateFileType.General;

	[Required]
	public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;

	public DateTimeOffset? DateUpdated { get; set; }

	public bool IsDeleted { get; set; }

	public DateTimeOffset? DateDeleted { get; set; }

	public enum EstimateFileType
	{
		General = 0,
		ProSignature = 1,
		CustomerSignature = 2,
		JobCompletionImage = 3
	}
}
