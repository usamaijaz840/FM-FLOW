using System.Globalization;
using System.Reflection;
using System.Text.Json;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FMFlow.Data.PostgresSeeder;

public class PostgresSQLSeederService(
	IConfiguration configuration,
	ILogger<PostgresSQLSeederService> logger,
	ISeedStatusService seedStatus) : BackgroundService
{
	private static readonly string SqlScriptsPath =
		Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!, "PostgresSeeder/");

	public string? FMFlowDBConnectionString => configuration.GetConnectionString("FMFlowDB");

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		seedStatus.IsComplete = false;

		logger.LogInformation("Waiting for PostgreSQL to be ready...");
		await WaitUntilPostgresIsUp(stoppingToken);

		var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
			.UseNpgsql(FMFlowDBConnectionString)
			.Options;

		await using var dbContext = new ApplicationDbContext(contextOptions);
		dbContext.Database.Migrate();

		if (stoppingToken.IsCancellationRequested) return;

		logger.LogInformation("Running PostgreSQL seed scripts...");

		try
		{
			await ExecuteDataLoads(stoppingToken);

			logger.LogInformation("PostgreSQL seeding completed.");
			seedStatus.IsComplete = true;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to initialize PostgreSQL.");
			// Leave IsComplete = false so health stays Unhealthy.
		}
	}

	private async Task ExecuteDataLoads(CancellationToken stoppingToken)
	{
		var dataFolder = Path.Combine(SqlScriptsPath, "DataToLoad");

		if (!Directory.Exists(dataFolder))
			return;

		var dataFiles = Directory.GetFiles(dataFolder, "*.csv").OrderBy(f => f);

		await using var connection = new NpgsqlConnection(FMFlowDBConnectionString);
		await connection.OpenAsync(stoppingToken);

		foreach (var filePath in dataFiles)
		{
			var fileName = Path.GetFileNameWithoutExtension(filePath);
			var tableName = fileName.Split('_')[1];
			var orderAndTableName = string.Join('_', fileName.Split('_').Take(2));

			if (!await DoesDataExist(connection, tableName))
			{
				logger.LogInformation($"Starting bulk load for table {tableName} from file {fileName}.csv...");

				try
				{
					await BulkLoadCsvToTable(connection, filePath, orderAndTableName, tableName, stoppingToken);
					logger.LogInformation($"Successfully loaded {fileName}.csv into {tableName}.");
				}
				catch (Exception ex)
				{
					logger.LogError(ex, $"Failed to load {fileName}.csv into {tableName}.");
				}
			}
			else
				logger.LogInformation($"Bulk load for table {tableName} from file {fileName}.csv was blocked due to existing records");
		}
	}

	private async Task BulkLoadCsvToTable(NpgsqlConnection connection, string csvFilePath, string orderAndTableName, string tableName, CancellationToken stoppingToken)
	{
		var columnMappings = await LoadColumnMappings(orderAndTableName);

		using var reader = new StreamReader(csvFilePath);
		using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

		await csv.ReadAsync();
		csv.ReadHeader();
		var csvHeaders = csv.HeaderRecord ?? throw new InvalidOperationException("CSV file is missing headers.");

		// Only include columns that are mapped
		var mappedColumns = csvHeaders.Where(h => columnMappings.ContainsKey(h));

		if (!mappedColumns.Any())
		{
			logger.LogWarning($"Skipping {csvFilePath} because no mapped columns match the table.");
			return;
		}

		var dbColumns = mappedColumns.Select(h => $"\"{columnMappings[h]}\"");

		var insertSql = new List<string>();

		while (await csv.ReadAsync())
		{
			var values = new List<string>();

			foreach (var column in mappedColumns)
			{
				var fieldValue = csv.GetField(column);

				if (string.IsNullOrEmpty(fieldValue))
				{
					values.Add("NULL");
				}
				else if (!fieldValue.StartsWith("0") && int.TryParse(fieldValue, out int intValue))
				{
					values.Add(intValue.ToString());
				}
				else if (!fieldValue.StartsWith("0") && decimal.TryParse(fieldValue, out decimal decimalValue))
				{
					values.Add(decimalValue.ToString(CultureInfo.InvariantCulture));
				}
				else if (bool.TryParse(fieldValue, out bool boolValue))
				{
					values.Add(boolValue ? "TRUE" : "FALSE");
				}
				else if (DateTime.TryParse(fieldValue, out DateTime dateTimeValue))
				{
					values.Add($"'{dateTimeValue:yyyy-MM-dd HH:mm:ss}'");
				}
				else
				{
					values.Add($"'{fieldValue.Replace("'", "''")}'");
				}
			}

			var sql = $"INSERT INTO \"{tableName}\" ({string.Join(", ", dbColumns)}) VALUES ({string.Join(", ", values)});";
			insertSql.Add(sql);
		}

		if (insertSql.Any())
		{
			var finalInsertQuery = string.Join("\n", insertSql);

			await using var command = new NpgsqlCommand(finalInsertQuery, connection);
			await command.ExecuteNonQueryAsync(stoppingToken);

			logger.LogInformation($"Successfully inserted {insertSql.Count} records into {tableName}.");
		}
		else
		{
			logger.LogWarning($"No valid records found in {csvFilePath} for insertion.");
		}
	}

	private async Task<Dictionary<string, string>> LoadColumnMappings(string tableName)
	{
		var mappingFilePath = $"{Path.GetDirectoryName(SqlScriptsPath)}/DataToLoad/{tableName}_MapToTable.json";

		if (!File.Exists(mappingFilePath))
		{
			logger.LogWarning($"Mapping file {mappingFilePath} not found. Using default column names.");
			return new Dictionary<string, string>(); // Return empty mapping (use CSV headers directly)
		}

		var jsonContent = await File.ReadAllTextAsync(mappingFilePath);
		var jsonData = JsonSerializer.Deserialize<ColumnMapping>(jsonContent);

		return jsonData?.Columns?.ToDictionary(c => c.ImportColumnName, c => c.TargetColumnName)
			   ?? new Dictionary<string, string>();
	}

	private class ColumnMapping
	{
		public List<ColumnMap> Columns { get; set; } = new List<ColumnMap>();
	}

	private class ColumnMap
	{
		public string ImportColumnName { get; set; } = string.Empty;
		public string TargetColumnName { get; set; } = string.Empty;
	}

	private async Task<bool> DoesDataExist(NpgsqlConnection connection, string tableName)
	{
		var query = $"SELECT EXISTS (SELECT 1 FROM \"{tableName}\" LIMIT 1);";
		await using var command = new NpgsqlCommand(query, connection);
		return (bool)(await command.ExecuteScalarAsync() ?? false);
	}

	private async Task WaitUntilPostgresIsUp(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				var systemConnectionString = new NpgsqlConnectionStringBuilder(FMFlowDBConnectionString)
				{
					Database = "postgres"
				}.ToString();
				await using var connection = new NpgsqlConnection(systemConnectionString);
				await connection.OpenAsync(stoppingToken);
				logger.LogInformation("PostgreSQL is ready!");
				return;
			}
			catch
			{
				logger.LogWarning("PostgreSQL not ready yet, retrying in 5 seconds...");
				await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
			}
		}
	}
}
