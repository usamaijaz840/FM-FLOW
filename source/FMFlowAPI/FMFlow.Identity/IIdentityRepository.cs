using FMFlow.Identity.Interface.DTOs;

namespace FMFlow.Identity.Interface;

public enum Roles
{
	AccountManager,
	Customer,
	Pro,
	Scheduler,
	SuperAdmin,
	TempCustomer,
	TempPro,
	ChatBot,
	EstimateRecipient
}

public interface IIdentityRepository
{
	Task<TokenResponseDto?> AuthenticateUser(string emailAddress, string password, CancellationToken ct);
	Task<Guid?> CreateUser(string email, string password, string firstName, string lastName, int? externalId, int? leadId, CancellationToken ct);
	Task<bool> DoesUserIdExist(string emailAddress, CancellationToken ct);
	Task SetUserPassword(string userId, string newPassword, CancellationToken ct);
	Task<List<KeycloakUser>?> SearchUsers(string searchTerm, int pageIndex, int pageSize, CancellationToken ct);
	Task<KeycloakRole?> GetRoleByName(Roles role, CancellationToken ct);
	Task<List<KeycloakUser>> GetUsersByRole(Roles role, CancellationToken ct, int? first = null, int? max = null);
	Task<List<Roles>> GetUserRoles(Guid identityGuid, CancellationToken ct);
	Task AssignRoleToUser(Guid userId, KeycloakRole role, CancellationToken ct);
	Task RemoveRoleFromUser(Guid userId, KeycloakRole role, CancellationToken ct);
	Task<TokenResponseDto?> RefreshAccessToken(string refreshToken, CancellationToken ct);
	string? GetEmailFromAccessToken(string accessToken);
	Task UpdateUserEmail(string userId, string newEmail, CancellationToken ct);
	Task<TokenResponseDto?> AuthenticateServiceClient(string clientId, string clientSecret, CancellationToken ct);
}

public class KeycloakUser
{
	public string Id { get; set; } = string.Empty;
	public string Username { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
}
