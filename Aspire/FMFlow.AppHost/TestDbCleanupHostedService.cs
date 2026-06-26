using Microsoft.Extensions.Hosting;
using Npgsql;

namespace FMFlow.AppHost;

internal sealed class TestDbCleanupHostedService : IHostedService
{
	private readonly string _host;
	private readonly int _port;
	private readonly string _username;
	private readonly string _password;
	private readonly string _currentBusinessDb;
	private readonly string _currentKeycloakDb;

	public TestDbCleanupHostedService(
		string host,
		int port,
		string username,
		string password,
		string currentBusinessDb,
		string currentKeycloakDb)
	{
		_host = host;
		_port = port;
		_username = username;
		_password = password;
		_currentBusinessDb = currentBusinessDb;
		_currentKeycloakDb = currentKeycloakDb;
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		var csb = new NpgsqlConnectionStringBuilder
		{
			Host = _host,
			Port = _port,
			Username = _username,
			Password = _password,
			Database = "postgres"
		};

		await WaitForPostgresAsync(csb.ConnectionString, cancellationToken);

		try
		{
			await using var conn = new NpgsqlConnection(csb.ConnectionString);
			await conn.OpenAsync(cancellationToken);

			const string sql = @"
				SELECT datname
				FROM pg_database
				WHERE datname LIKE 'fmflowdb_test_%'
				   OR datname LIKE 'keycloakdb_test_%';";

			var toDrop = new List<string>();

			await using (var cmd = new NpgsqlCommand(sql, conn))
			await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
			{
				while (await reader.ReadAsync(cancellationToken))
				{
					var name = reader.GetString(0);
					if (!name.Equals(_currentBusinessDb, StringComparison.OrdinalIgnoreCase) &&
						!name.Equals(_currentKeycloakDb, StringComparison.OrdinalIgnoreCase))
					{
						toDrop.Add(name);
					}
				}
			}

			foreach (var db in toDrop)
			{
				try
				{
					await using (var term = new NpgsqlCommand("SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @db;", conn))
					{
						term.Parameters.AddWithValue("db", db);
						await term.ExecuteNonQueryAsync(cancellationToken);
					}

					try
					{
						await using var dropForce = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{db}\" WITH (FORCE);", conn);
						await dropForce.ExecuteNonQueryAsync(cancellationToken);
					}
					catch
					{
						await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{db}\";", conn);
						await drop.ExecuteNonQueryAsync(cancellationToken);
					}
				}
				catch
				{
					// ignore individual failures
				}
			}
		}
		catch
		{
			// ignore cleanup failure
		}
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

	private static async Task WaitForPostgresAsync(string adminConnStr, CancellationToken ct)
	{
		var timeout = DateTime.UtcNow.AddMinutes(2);
		while (DateTime.UtcNow < timeout && !ct.IsCancellationRequested)
		{
			try
			{
				await using var c = new NpgsqlConnection(adminConnStr);
				await c.OpenAsync(ct);
				return;
			}
			catch
			{
				await Task.Delay(1000, ct);
			}
		}
	}
}
