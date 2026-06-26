using EFRepository;
using FMFlow.Admin.Interface;
using FMFlow.Admin.Interface.DTOs;
using FMFlow.Admin.Service.Mappers;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.ProUser.Interface;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.Admin.Service;

public class AdminService(IRepository repository) : IAdminService
{
	public async Task<Result<SearchedProUserDetails>> GetProUserDetailsByUserId(int userId, CancellationToken ct)
	{
		var proUser = await repository.Query<ProUserDetail>()
			.ByUserID(userId)
			.Include(p => p.FlowUser)
			.Include(p => p.FMTimeZone)
			.Include(p => p.BusinessAddress)
			.Include(p => p.SherwinHomeAddress)
			.FirstOrDefaultAsync(ct);

		if (proUser == null)
		{
			return Result<SearchedProUserDetails>.Failure(ErrorMessages.UserNotFound, ResultErrorType.NotFound);
		}

		var mapper = new UserMapper();

		var dto = mapper.MapToSearchedProUserDetails(proUser);

		return Result<SearchedProUserDetails>.Success(dto);
	}

	public async Task<Result> UpdateUserDeletedStatus(int userID, bool isDeleted, CancellationToken ct)
	{
		var foundUser = await repository.Query<FlowUser>()
			.ByUserID(userID)
			.AsNoTracking()
			.FirstOrDefaultAsync(ct);

		if (foundUser == null)
		{
			return Result.Failure(ErrorMessages.UserNotFound, ResultErrorType.NotFound);
		}

		foundUser.IsDeleted = isDeleted;

		if (isDeleted)
		{
			foundUser.DateDeleted = DateTime.UtcNow;
		}
		else
		{
			foundUser.DateDeleted = null;
		}

		repository.AddOrUpdate(foundUser);
		await repository.SaveAsync(ct);

		return Result.Success();
	}

	public async Task<Result<AccountManagerZipCodesDto>> GetAccountManagerZipCodes(int accountManagerID, CancellationToken ct)
	{
		var accountManager = await repository.Query<FlowUser>()
			.Where(u => u.UserID == accountManagerID)
			.Include(u => u.EmployeeUser)
			.ThenInclude(eu => eu!.AssignedZipCodes)
			.ThenInclude(z => z.State)
			.FirstOrDefaultAsync(ct);

		if (accountManager?.EmployeeUser == null)
		{
			return Result<AccountManagerZipCodesDto>.Failure("Account Manager not found", ResultErrorType.NotFound);
		}

		var zipCodesGrouped = accountManager.EmployeeUser.AssignedZipCodes
			.GroupBy(z => z.State)
			.Select(stateGroup => new StateZipCodesDto
			{
				Name = stateGroup.Key.StateName,
				Abbreviation = stateGroup.Key.Abbreviation,
				Counties = stateGroup
					.GroupBy(z => z.County)
					.Select(countyGroup => new CountyZipCodesDto
					{
						Name = countyGroup.Key,
						ZipCodes = countyGroup.Select(z => z.Zipcode).OrderBy(z => z).ToList()
					})
					.OrderBy(c => c.Name)
					.ToList()
			})
			.OrderBy(s => s.Name)
			.ToList();

		var result = new AccountManagerZipCodesDto
		{
			States = zipCodesGrouped
		};

		return Result<AccountManagerZipCodesDto>.Success(result);
	}

	public async Task<Result> AddZipCodesToAccountManager(int accountManagerID, List<string> zipCodes, CancellationToken ct)
	{
		var accountManager = await repository
			.Query<FlowUser>()
			.ByUserID(accountManagerID)
			.Include(x => x.EmployeeUser != null ? x.EmployeeUser.AssignedZipCodes : null)
			.FirstOrDefaultAsync(ct);

		if (accountManager == null || accountManager.EmployeeUser == null)
			return Result.Failure("Account manager does not exist", ResultErrorType.NotFound);

		var validZipCodes = await repository.Query<ZipCode>()
			.Where(z => zipCodes.Contains(z.Zipcode))
			.ToListAsync(ct);

		if (validZipCodes.Count != zipCodes.Count)
			return Result.Failure("One or more invalid zip codes.");

		var existingZipCodes = accountManager.EmployeeUser.AssignedZipCodes.Select(z => z.Zipcode).ToList();
		var zipCodesToAdd = zipCodes.Except(existingZipCodes).ToList();

		if (zipCodesToAdd.Count == 0)
			return Result.Failure("All specified zip codes are already assigned to this account manager.");

		accountManager.EmployeeUser.AssignedZipCodes.AddRange(validZipCodes.Where(z => zipCodesToAdd.Contains(z.Zipcode)));

		repository.AddOrUpdate(accountManager.EmployeeUser);
		await repository.SaveAsync(ct);

		return Result.Success();
	}

	public async Task<Result> RemoveZipCodesFromAccountManager(int accountManagerID, List<string> zipCodes, CancellationToken ct)
	{
		var accountManager = await repository
			.Query<FlowUser>()
			.ByUserID(accountManagerID)
			.Include(x => x.EmployeeUser != null ? x.EmployeeUser.AssignedZipCodes : null)
			.FirstOrDefaultAsync(ct);

		if (accountManager == null || accountManager.EmployeeUser == null)
			return Result.Failure("Account manager does not exist", ResultErrorType.NotFound);

		var existingZipCodes = accountManager.EmployeeUser.AssignedZipCodes.Select(z => z.Zipcode).ToList();
		var zipCodesToRemove = zipCodes.Intersect(existingZipCodes).ToList();

		if (zipCodesToRemove.Count == 0)
			return Result.Failure("None of the specified zip codes are currently assigned to this account manager.");

		accountManager.EmployeeUser.AssignedZipCodes.RemoveAll(z => zipCodesToRemove.Contains(z.Zipcode));

		repository.AddOrUpdate(accountManager.EmployeeUser);
		await repository.SaveAsync(ct);

		return Result.Success();
	}
}
