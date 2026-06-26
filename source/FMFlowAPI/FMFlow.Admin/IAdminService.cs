using FMFlow.Admin.Interface.DTOs;
using FMFlow.FlowAPI.Interface;
using FMFlow.ProUser.Interface;

namespace FMFlow.Admin.Interface;

public interface IAdminService
{
	Task<Result<SearchedProUserDetails>> GetProUserDetailsByUserId(int userID, CancellationToken ct);

	Task<Result> UpdateUserDeletedStatus(int userID, bool isDeleted, CancellationToken ct);

	Task<Result<AccountManagerZipCodesDto>> GetAccountManagerZipCodes(int accountManagerID, CancellationToken ct);

	Task<Result> AddZipCodesToAccountManager(int accountManagerID, List<string> zipCodes, CancellationToken ct);

	Task<Result> RemoveZipCodesFromAccountManager(int accountManagerID, List<string> zipCodes, CancellationToken ct);
}
