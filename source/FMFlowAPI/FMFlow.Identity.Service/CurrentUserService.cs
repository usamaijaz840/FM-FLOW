using System.Security.Claims;
using FMFlow.Entities;
using FMFlow.Identity.Interface;
using Microsoft.AspNetCore.Http;

namespace FMFlow.Identity.Service;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor, IIdentityService identityService) : ICurrentUserService
{
	private FlowUser? _cachedUser;

	public string? Email => httpContextAccessor.HttpContext?.User.FindFirstValue("preferred_username");

	public int GetUserID()
	{
		var externalIdClaim = httpContextAccessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == "external_id")?.Value;

		return int.TryParse(externalIdClaim, out var userId) ? userId : throw new UnauthorizedAccessException("User id missing in token.");
	}

	public int GetLeadId()
	{
		var leadIdClaim = httpContextAccessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == "lead_id")?.Value;

		return int.TryParse(leadIdClaim, out var userId) ? userId : throw new UnauthorizedAccessException("Lead id missing in token.");
	}

	public bool IsInRole(string role) =>
		httpContextAccessor.HttpContext?.User.IsInRole(role) == true;

	public async Task<FlowUser?> GetCurrentUser(CancellationToken ct)
	{
		if (_cachedUser != null)
		{
			return _cachedUser;
		}

		if (Email is null)
		{
			return null;
		}

		_cachedUser = await identityService.GetUserProfileByEmail(Email, ct);

		return _cachedUser;
	}

	public bool IsAccountManager() => IsInRole(nameof(Roles.AccountManager));
	public bool IsCustomer() => IsInRole(nameof(Roles.Customer));
	public bool IsTempCustomer() => IsInRole(nameof(Roles.TempCustomer));
	public bool IsScheduler() => IsInRole(nameof(Roles.Scheduler));
	public bool IsSuperAdmin() => IsInRole(nameof(Roles.SuperAdmin));
	public bool IsPro() => IsInRole(nameof(Roles.Pro));
	public bool IsTempPro() => IsInRole(nameof(Roles.TempPro));
	public bool IsChatBot() => IsInRole(nameof(Roles.ChatBot));
	public bool IsEstimateRecipient() => IsInRole(nameof(Roles.EstimateRecipient));

	public bool IsFMEmployee() => IsSuperAdmin() || IsAccountManager() || IsScheduler();
}

