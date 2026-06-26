namespace FMFlow.Common;

public class AppSettings
{
	public const string SectionName = "App";
	public string Frontend { get; set; } = string.Empty;
	public string BaseApiUrl { get; set; } = string.Empty;
}
