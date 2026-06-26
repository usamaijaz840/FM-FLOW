using FMFlow.Entities;

namespace FMFlow.Identity.Interface;

public interface ICurrentUserService
{
	string? Email { get; }
	int GetUserID();
	int GetLeadId();
	bool IsInRole(string role);
	Task<FlowUser?> GetCurrentUser(CancellationToken ct);
	bool IsAccountManager();
	bool IsCustomer();
	bool IsTempCustomer();
	bool IsScheduler();
	bool IsSuperAdmin();
	bool IsPro();
	bool IsTempPro();
	bool IsChatBot();
	bool IsEstimateRecipient();
	bool IsFMEmployee();
}
