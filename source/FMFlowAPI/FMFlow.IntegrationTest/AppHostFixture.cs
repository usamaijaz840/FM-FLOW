using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Hosting;
using Microsoft.Extensions.Logging;

namespace FMFlow.IntegrationTest;

[CollectionDefinition(Name, DisableParallelization = true)]
public class AppHostCollection : ICollectionFixture<AppHostFixture>
{
	public const string Name = "AppHostCollection";
}

public sealed class AppHostFixture : IAsyncLifetime
{
	public DistributedApplication App { get; private set; } = default!;

	public HttpClient ApiClient { get; private set; } = default!;

	// Shared JSON options matching API (string enums in camelCase)
	public JsonSerializerOptions JsonOptions { get; } = new()
	{
		PropertyNameCaseInsensitive = true
	};

	private string? _superAdminAccessToken;
	private DateTimeOffset _superAdminTokenExpiresUtc;

	public async Task InitializeAsync()
	{
		// one run id for the whole test session
		Environment.SetEnvironmentVariable("ASPIRE_TEST", "true");
		Environment.SetEnvironmentVariable("ASPIRE_TEST_RUN_ID",
			Environment.GetEnvironmentVariable("ASPIRE_TEST_RUN_ID") ?? Guid.NewGuid().ToString("N"));

		var host = await DistributedApplicationTestingBuilder.CreateAsync<global::Projects.FMFlow_AppHost>();

		host.Services.AddLogging(lb =>
		{
			lb.SetMinimumLevel(LogLevel.Debug);
			lb.AddFilter("Aspire.", LogLevel.Debug);
		});

#if DEBUG
		// Extend timeouts for debugging
		host.Services.ConfigureHttpClientDefaults(b =>
		{
			b.AddStandardResilienceHandler(options =>
			{
				options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(15);
				options.AttemptTimeout.Timeout = TimeSpan.FromMinutes(15);
				options.CircuitBreaker.SamplingDuration = TimeSpan.FromHours(1);
				options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(1);
				options.CircuitBreaker.MinimumThroughput = int.MaxValue;
				options.CircuitBreaker.FailureRatio = 0.999999;
			});
		});
#else
		host.Services.ConfigureHttpClientDefaults(b => b.AddStandardResilienceHandler());
#endif

		// Match server enum serialization (camelCase strings)
		JsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

		App = await host.BuildAsync();
		await App.StartAsync();

		// wait for the API and Keycloak to be healthy once
		await App.ResourceNotifications.WaitForResourceHealthyAsync("keycloak", CancellationToken.None);
		await App.ResourceNotifications.WaitForResourceHealthyAsync("fmflowapi", CancellationToken.None);

		ApiClient = App.CreateHttpClient("fmflowapi");

#if DEBUG
		ApiClient.Timeout = TimeSpan.FromMinutes(15);
#endif
	}

	public async Task EnsureSuperAdminAccessToken(CancellationToken ct)
	{
		if (_superAdminAccessToken is not null && _superAdminTokenExpiresUtc > DateTimeOffset.UtcNow.AddMinutes(1))
		{
			// still valid (with a 1 minute safety window)
			return;
		}

		// Use the internal Keycloak endpoint inside the Aspire network.
		var keycloakClient = App.CreateHttpClient("keycloak"); // resolves to http://keycloak:8080

#if DEBUG
		keycloakClient.Timeout = TimeSpan.FromMinutes(15);
#endif

		// Password grant against the fm-flow-api client in the fmflow realm.
		// NOTE: In your test realm JSON, fm-flow-api is confidential and has this client secret.
		// If you change it in the realm, update it here as well.
		var form = new Dictionary<string, string>
		{
			["grant_type"] = "password",
			["client_id"] = "fm-flow-api",
			["client_secret"] = "kpDcwsRSjNb7P41mdu9csgqo1NE7nPmi",
			["username"] = "superadmin@test.com",
			["password"] = "AdminPass123!" // this matches fmflow-realm-test.json
		};

		using var response = await keycloakClient.PostAsync(
			"/realms/fmflow/protocol/openid-connect/token",
			new FormUrlEncodedContent(form),
			ct);

		await response.EnsureSuccessStatusCodeWithContent(ct);

		var json = await response.Content.ReadAsStringAsync(ct);
		using var doc = JsonDocument.Parse(json);
		var accessToken = doc.RootElement.GetProperty("access_token").GetString();

		if (string.IsNullOrEmpty(accessToken))
		{
			throw new InvalidOperationException("Keycloak did not return an access_token.");
		}

		_superAdminAccessToken = accessToken;

		// Assume a fixed lifetime of 14 minutes for tests.
		_superAdminTokenExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(14);

		ApiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
	}

	public async Task EnsureSeedingComplete(CancellationToken ct)
	{
		// Poll the SeedController: GET /api/seed -> returns true/false (JSON boolean)
		var max = TimeSpan.FromMinutes(2);
		var start = DateTime.UtcNow;

		while (true)
		{
			ct.ThrowIfCancellationRequested();

			try
			{
				using var response = await ApiClient.GetAsync("/api/seed", ct);

				if (response.IsSuccessStatusCode)
				{
					var body = await response.Content.ReadAsStringAsync(ct);

					if (bool.TryParse(body, out var complete) && complete)
						return;
				}
			}
			catch
			{
				// Ignore transient issues while API warms up; continue polling.
			}

			if (DateTime.UtcNow - start > max)
				throw new TimeoutException("Seeding did not complete within the allotted time.");

			await Task.Delay(1000, ct);
		}
	}

	public async Task DisposeAsync()
	{
		await App.DisposeAsync();
	}
}
