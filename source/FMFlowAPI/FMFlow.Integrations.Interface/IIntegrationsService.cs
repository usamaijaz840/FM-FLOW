using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Integrations.Interface.DTOs;

namespace FMFlow.Integrations.Interface;

public interface IIntegrationsService
{
	Task<Result<List<IntegrationResponseDto>>> GetIntegrations(CancellationToken ct);
	Task<Result> DeleteIntegration(IntegrationType integrationType, CancellationToken ct);
	Task<Result<string?>> HandleOutlookCalendarIntegration(string? code, string? state, CancellationToken ct);
	Task<Result<string?>> HandleGoogleCalendarIntegration(string? code, string? state, CancellationToken ct);
	Task<Result<string>> VerifyIntegrationStatus(IntegrationType? integrationType, CancellationToken ct);
}
