
namespace FMFlow.Files.Interface.DTOs;

public record FileUploadSettings
{
	public string S3BucketName { get; set; } = string.Empty;
	public string Region { get; set; } = string.Empty;
	public string S3AccessPointArn { get; set; } = string.Empty;
	public long MaxFileSize { get; set; }
	public string AccessKeyID { get; set; } = string.Empty;
	public string SecrectAccessKey { get; set; } = string.Empty;

}
