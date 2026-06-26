
namespace FMFlow.ProUser;

public record ProZipcode
{
	public string Zipcode { get; set; } = string.Empty;
	public string StateAbbreviation { get; set; } = string.Empty;
	public string StateName { get; set; } = string.Empty;
	public string County { get; set; } = string.Empty;
}
