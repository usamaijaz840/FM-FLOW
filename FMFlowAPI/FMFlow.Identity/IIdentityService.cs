using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Identity.Interface.DTOs;

namespace FMFlow.Identity.Interface;

public interface IIdentityService
{
	Task<Result> AssignUserToRole(int userId, Roles role, CancellationToken ct);

	Task<Result<int>> CreateUser(FlowUser info, CancellationToken ct, int? leadId = null, string? password = null);

	Task<Result<CreateTempCustomerDto>> CreateTempCustomer(FlowUser newCustomerUser, int leadId, CancellationToken ct);

	Task<Result<TokenResponseDto>> SaveUserPassword(int userId, string password, CancellationToken ct);

	Task<FlowUser?> GetUserProfileByEmail(string email, CancellationToken ct);

	Task<Result> RemoveRoleFromUser(int userId, Roles role, CancellationToken ct);

	Task<Result> UpdateUserDeletedStatus(int userId, bool isDeleted, CancellationToken ct);

	Task<Result<TokenResponseDto>> AuthenticateServiceClient(ServiceAccountClientTokenRequestDto request, CancellationToken ct);
}
