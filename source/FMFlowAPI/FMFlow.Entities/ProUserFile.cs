using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace FMFlow.Entities;

public class ProUserFile
{
	[Key]
	public int ProUserFileID { get; set; }

	[ForeignKey(nameof(File))]
	public int FileID { get; set; }

	[ForeignKey(nameof(ThumbnailFile))]
	public int? ThumbnailFileID { get; set; }
	public ProUserFileType ProFileType { get; set; }

	[ForeignKey(nameof(FlowUser))]
	public int UserID { get; set; }

	public virtual FlowUser? FlowUser { get; set; }

	public FileItem File { get; set; }

	public FileItem? ThumbnailFile { get; set; }

	[Required]
	public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;
}

public enum ProUserFileType
{
	Logo = 0,
	Insurance = 1,
	CertificateOrLicense = 2,
	OtherDocument = 3,
	ProfilePicture = 4,
}
