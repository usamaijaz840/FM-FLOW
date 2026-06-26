using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

var isTest = Environment.GetEnvironmentVariable("ASPIRE_TEST") == "true";
var testRunId = Environment.GetEnvironmentVariable("ASPIRE_TEST_RUN_ID");

// Resolve database names
var postgresDbNameValue =
	isTest && !string.IsNullOrWhiteSpace(testRunId)
		? $"fmflowdb_test_{testRunId}"
		: (isTest ? "fmflowdb_test" : "fmflowdb");

var keycloakDbNameValue =
	isTest && !string.IsNullOrWhiteSpace(testRunId)
		? $"keycloakdb_test_{testRunId}"
		: (isTest ? "keycloakdb_test" : "keycloakdb");

// Parameters with defaults
var postgresUsernameParam = builder.AddParameter("postgresUsername", secret: true);
var postgresPasswordParam = builder.AddParameter("postgresPassword", secret: true);
var postgresPortParam = builder.AddParameter("postgresPort", "5432");
var postgresHostParam = builder.AddParameter("postgresHost", "localhost");
var postgresDbNameParam = builder.AddParameter("postgresDBName", postgresDbNameValue);
var keycloakDbNameParam = builder.AddParameter("keycloakDBName", keycloakDbNameValue);
var postgresIncludeErrorDetailsParam = builder.AddParameter("ShouldIncludeErrorDetails", "false");

// Resolve needed values
var postgresPortString = await postgresPortParam.Resource.GetValueAsync(CancellationToken.None);
var postgresPort = string.IsNullOrWhiteSpace(postgresPortString) ? 5432 : int.Parse(postgresPortString);
var postgresHost = await postgresHostParam.Resource.GetValueAsync(CancellationToken.None) ?? "localhost";
var postgresUsername = await postgresUsernameParam.Resource.GetValueAsync(CancellationToken.None) ?? "";
var postgresPassword = await postgresPasswordParam.Resource.GetValueAsync(CancellationToken.None) ?? "";
var postgresDbName = await postgresDbNameParam.Resource.GetValueAsync(CancellationToken.None) ?? postgresDbNameValue;
var keycloakDbName = await keycloakDbNameParam.Resource.GetValueAsync(CancellationToken.None) ?? keycloakDbNameValue;
var includeErrorDetail = await postgresIncludeErrorDetailsParam.Resource.GetValueAsync(CancellationToken.None) ?? "false";

if (isTest)
{
	builder.Services.AddHostedService(_ =>
		new FMFlow.AppHost.TestDbCleanupHostedService(
			postgresHost,
			postgresPort,
			postgresUsername,
			postgresPassword,
			postgresDbName,
			keycloakDbName));
}

// Determine Postgres host name for other containers
string postgresContainerHost;

var postgresBuilder = builder.AddPostgres("postgres", postgresUsernameParam, postgresPasswordParam);

if (isTest && !string.IsNullOrWhiteSpace(testRunId))
{
	// Keep unique container name but host alias inside docker network is still 'postgres'
	postgresBuilder = postgresBuilder.WithContainerName($"FMFlowPostgresServer_{testRunId}");
	postgresContainerHost = "postgres";
}
else if (!isTest)
{
	postgresBuilder = postgresBuilder.WithContainerName("FMFlowPostgresServer");
	postgresContainerHost = "FMFlowPostgresServer";
}
else
{
	postgresContainerHost = "postgres";
}

// Optional logging only
postgresBuilder = postgresBuilder.WithEnvironment("PGOPTIONS", includeErrorDetail == "true" ? "-c log_statement=all" : null);

postgresBuilder = postgresBuilder.WithEndpoint(name: "postgresendpoint", scheme: "tcp", targetPort: postgresPort, isProxied: false);

if (!isTest)
{
	postgresBuilder = postgresBuilder
		.WithLifetime(ContainerLifetime.Persistent)
		.WithDataVolume("FMFlowDB", isReadOnly: false);
}

var postgres = postgresBuilder;

// Databases
var FMFlowDB = postgres.AddDatabase("FMFlowDB", postgresDbName);
var keycloakDB = postgres.AddDatabase("KeycloakDB", keycloakDbName);

if (isTest)
{
	postgresBuilder = postgresBuilder
		.WithBindMount("./postgres-init", "/docker-entrypoint-initdb.d", isReadOnly: true)
		.WithEnvironment("KEYCLOAK_DBNAME", keycloakDbName);
}

var keycloakBuilder = builder.AddKeycloak("keycloak", 8080);

if (isTest && !string.IsNullOrWhiteSpace(testRunId))
{
	keycloakBuilder = keycloakBuilder.WithContainerName($"FMFlowKeycloak_{testRunId}");
}
else if (!isTest)
{
	keycloakBuilder = keycloakBuilder.WithContainerName("FMFlowKeycloak");
}

keycloakBuilder = keycloakBuilder
	.WithReference(keycloakDB)
	.WaitFor(keycloakDB) // ensure keycloak DB exists before starting
	.WithReference(postgres)
	.WaitFor(postgres)
	.WithEnvironment("KC_DB_URL_HOST", postgresContainerHost)
	.WithEnvironment("KC_DB_URL_DATABASE", keycloakDbName)
	.WithEnvironment("KC_DB_URL_PORT", postgresPort.ToString())
	.WithEnvironment("KC_DB_USERNAME", postgresUsername)
	.WithEnvironment("KC_DB_PASSWORD", postgresPassword)
	.WithEnvironment("KC_DB", "postgres");

if (isTest)
{
	keycloakBuilder = keycloakBuilder
		.WithRealmImport("./KeycloakRealms.Test")
		.WithEnvironment("KC_HTTP_ENABLED", "true")
		.WithEnvironment("KC_PROXY", "edge")
		.WithEnvironment("KC_HOSTNAME", "keycloak")
		.WithEnvironment("KC_HOSTNAME_PORT", "8080")
		.WithEnvironment("KC_HOSTNAME_STRICT", "true")
		.WithEnvironment("KC_HOSTNAME_STRICT_HTTPS", "false")
		.WithEnvironment("KC_HOSTNAME_URL", "http://keycloak:8080")
		.WithEnvironment("KC_HOSTNAME_ADMIN_URL", "http://keycloak:8080");
}
else
{
	keycloakBuilder = keycloakBuilder
		.WithRealmImport("./KeycloakRealms")
		.WithLifetime(ContainerLifetime.Persistent);
}

var keycloak = keycloakBuilder;

// API project
var apiBuilder = builder.AddProject<Projects.FMFlowAPI>("fmflowapi")
	.WithReference(FMFlowDB)
	.WithReference(postgres)
	.WaitFor(postgres)
	.WithReference(keycloak)
	.WaitFor(keycloak)
	.WithExternalHttpEndpoints()
	.WithEnvironment("APP_VERSION", "local-dev")
	.WithEnvironment("DEPLOY_TIME", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));

if (isTest)
{
	apiBuilder = apiBuilder
		.WithEnvironment("Keycloak__InternalUrl", "http://keycloak:8080")
		.WithEnvironment("Keycloak__RequireHttpsMetadata", "false");
}

// add Vite frontend via Nx workspace script at repo root
builder.AddNpmApp("FMFlowWeb", "../../../", "dev")
	.WithHttpEndpoint(targetPort: 3000, name: "web", isProxied: false)
	.WithEnvironment("VITE_DISABLE_MSW", "true")
	.WithEnvironment("VITE_APP_VERSION", "local-dev")
	.WithEnvironment("VITE_DEPLOY_TIME", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));

builder.Build().Run();
