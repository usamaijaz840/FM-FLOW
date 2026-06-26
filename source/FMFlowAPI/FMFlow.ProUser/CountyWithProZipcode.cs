namespace FMFlow.ProUser;
public record CountyWithProZipcode
{
	public string Name { get; init; } = string.Empty;
	public List<string> ZipCodes { get; set; } = new List<string>();
}
