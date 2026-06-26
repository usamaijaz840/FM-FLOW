namespace FMFlow.Data.PostgresSeeder;

public interface ISeedStatusService
{
	bool IsComplete { get; set; }
}

public sealed class SeedStatusService : ISeedStatusService
{
	public bool IsComplete { get; set; }
}
