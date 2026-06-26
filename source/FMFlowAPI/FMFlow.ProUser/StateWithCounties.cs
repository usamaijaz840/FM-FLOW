namespace FMFlow.ProUser;

public record StateWithCounties()
{
	public string Abbreviation { get; init; }
	public string Name { get; init; }
	public List<CountyWithProZipcode> Counties { get; init; }

}
