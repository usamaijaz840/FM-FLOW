using FMFlow.Integrations.Interface;
using FMFlow.Integrations.Service.Mapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FMFlow.Integrations.Service;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddIntegrationServices(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<GoogleSettings>(configuration.GetSection(GoogleSettings.SectionName));
		services.Configure<OutlookSettings>(configuration.GetSection(OutlookSettings.SectionName));
		
		// Register IntegrationsService with typed HttpClient
		services.AddHttpClient<IIntegrationsService, IntegrationsService>((sp, client) =>
		{
			client.Timeout = TimeSpan.FromSeconds(30);
		});

		// Register TokenRefreshService with HttpClient
		services.AddHttpClient<ITokenRefreshService, TokenRefreshService>((sp, client) =>
		{
			client.Timeout = TimeSpan.FromSeconds(30);
		});

		services.AddScoped<ICalendarService, GoogleCalendarService>();
		services.AddScoped<ICalendarService, OutlookCalendarService>();
		services.AddScoped<IGraphApiClient, GraphApiClient>();

		// Add PlacesMapper
		services.AddSingleton<PlacesMapper>();

		// Conditionally register PlacesService (mock or real) based on configuration
		var googleSettings = configuration.GetSection(GoogleSettings.SectionName).Get<GoogleSettings>();
		if (googleSettings?.UseMockPlacesService == true)
		{
			services.AddScoped<IPlacesService, MockPlacesService>();
		}
		else
		{
			// Add PlacesService with HttpClient
			services.AddHttpClient<IPlacesService, PlacesService>((sp, client) =>
			{
				// Configure HttpClient with timeout
				client.Timeout = TimeSpan.FromSeconds(30);
			});
		}

		return services;
	}
}
