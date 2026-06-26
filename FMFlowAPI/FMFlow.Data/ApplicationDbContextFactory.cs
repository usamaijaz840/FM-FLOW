using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FMFlow.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
	public ApplicationDbContext CreateDbContext(string[] args)
	{
		IConfigurationRoot configuration = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.json")
			.Build();

		var connectionString = configuration.GetConnectionString("FMFlowDB");
		connectionString = "Host=localhost;Port=5432;Database=fmflowdb;Username=postgres_user;Password=sun-bank-out-over-jake-3;";
		Console.WriteLine("connectionString: " + connectionString);
		var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
		optionsBuilder.UseNpgsql(connectionString);

		return new ApplicationDbContext(optionsBuilder.Options);
	}
}
