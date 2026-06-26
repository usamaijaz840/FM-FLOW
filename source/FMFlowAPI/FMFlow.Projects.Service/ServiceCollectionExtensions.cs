using FMFlow.Projects.Interface;
using FMFlow.Projects.Interface.DTOs;
using FMFlow.Projects.Service.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FMFlow.Projects.Service
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddProjectServices(this IServiceCollection services)
        {
            // Register project-related services
            services.AddScoped<IProjectsService, ProjectsService>();
            services.AddScoped<IProjectNotesService, ProjectNotesService>();
            
            // Register validators
            services.AddScoped<IValidator<ProjectRequestDto>, ProjectRequestDtoValidator>();
            services.AddScoped<IValidator<ProjectNoteRequestDto>, ProjectNoteRequestDtoValidator>();
            
            return services;
        }
    }
}
