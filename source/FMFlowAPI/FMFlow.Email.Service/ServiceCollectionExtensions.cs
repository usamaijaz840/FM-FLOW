using FMFlow.Email.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FMFlow.Email.Service
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddEmailServices(this IServiceCollection services, IConfiguration configuration)
		{
			services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
			services.AddSingleton<IEmailTemplateRenderer, HandlebarsTemplateRenderer>();
			services.Configure<EmailStaticDataSettings>(configuration.GetSection(EmailStaticDataSettings.SectionName));
			services.Configure<EmailTemplateIdsSettings>(configuration.GetSection(EmailTemplateIdsSettings.SectionName));
			services.AddScoped<IEmailService, SendGridEmailService>();
			services.AddScoped<IEmailSenderService, EmailSenderService>();

			return services;
		}
	}
}
