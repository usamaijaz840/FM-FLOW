using FMFlow.Common;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Leads.Interface.DTOs;

namespace FMFlow.Leads.Interface;

public interface ILeadsService
{
	Task<Result<LeadResponseDto>> CreateLead(LeadRequestDto createLeadRequest, CancellationToken ct);

	Task<Result<SearchResult<LeadResponseDto>>> SearchLeads(
		string? keywordSearch,
		bool? uncategorizedLeads,
		bool? includeScheduleComplete,
		int pageIndex,
		int pageSize,
		CustomerType? customerType,
		bool returnAllLeads,
		CancellationToken ct);

	Task<Result<LeadResponseDto>> UpdateLead(int leadId, LeadUpdateRequestDto leadRequest, CancellationToken ct);

	Task<Result> DeleteLead(int leadId, CancellationToken ct);

	Task<Result<LeadResponseDto>> GetLeadById(int leadId, CancellationToken ct);

	/// <summary>
	/// Ensures that the given lead has an associated customer user.
	/// If the lead does not have a customer, it attempts to find an existing user by email.
	/// If no existing user is found, a new customer user is created in the FMFlowUser database only (not in Keycloak) and linked to the lead.
	/// </summary>
	/// <param name="lead">The lead to ensure has a customer.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>A Result indicating success or failure.</returns>
	Task<Result> EnsureLeadHasCustomer(Lead lead, CancellationToken ct);
}
