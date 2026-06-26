using EFRepository;
using FluentValidation;
using FMFlow.Common;
using FMFlow.Employees.Interface;
using FMFlow.Employees.Interface.DTOs;
using FMFlow.Employees.Service.Mapper;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Identity.Interface;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.Employees.Service;

public class EmployeeService(
	IRepository repository,
	IIdentityService identityService,
	IIdentityRepository identityRepository,
	IValidator<EmployeeRequestDto> validator) : IEmployeeService
{
	public async Task<Result<EmployeeResponseDto>> CreateEmployeeUser(EmployeeRequestDto request, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		var validationResult = await DtoValidator.Validate(request, validator, ct);

		if (!validationResult.IsSuccess)
		{
			return Result<EmployeeResponseDto>.Failure(validationResult.Error!);
		}

		ct.ThrowIfCancellationRequested();

		var userRole = Enum.Parse<Roles>(request.Role);

		var mapper = new EmployeeMapper();
		var user = mapper.MapToEmployeeUser(request);

		if (user.EmployeeUser == null)
		{
			return Result<EmployeeResponseDto>.Failure("Failed to map request to user.", ResultErrorType.BadRequest);
		}

		if (request.AddressLine1 != null &&
			request.City != null &&
			request.State != null &&
			request.ZipCode != null)
		{
			var state = await repository
				.Query<State>()
				.ByAbbreviation(request.State)
				.FirstOrDefaultAsync(ct);

			if (state == null)
			{
				return Result<EmployeeResponseDto>.Failure("State not found.", ResultErrorType.NotFound);
			}

			// Create and save the Address first
			user.EmployeeUser.Address = mapper.CreateAddressFromEmployeeRequest(request, state);
		}

		user.UserID = null;
		ct.ThrowIfCancellationRequested();
		var createUserResult = await identityService.CreateUser(user, ct);

		if (!createUserResult.IsSuccess)
		{
			return Result<EmployeeResponseDto>.Failure(createUserResult.Error!);
		}

		user.UserID = createUserResult.Value;
		user.EmployeeUser.UserID = createUserResult.Value;

		ct.ThrowIfCancellationRequested();

		var assignRoleResult = await identityService.AssignUserToRole(user.EmployeeUser.UserID, userRole, ct);

		if (!assignRoleResult.IsSuccess)
		{
			return Result<EmployeeResponseDto>.Failure(assignRoleResult.Error!, assignRoleResult.ErrorType);
		}

		await repository.SaveAsync(ct);

		var result = mapper.MapToEmployeeDtoFromEmployeeUser(user.EmployeeUser);

		result.UserID = user.UserID ?? 0;

		return Result<EmployeeResponseDto>.Success(result);
	}

	public async Task<Result<EmployeeResponseDto>> UpdateEmployeeUser(int employeeId, EmployeeRequestDto request, CancellationToken ct)
	{
		var validationResult = await DtoValidator.Validate(request, validator, ct);

		if (!validationResult.IsSuccess)
		{
			return Result<EmployeeResponseDto>.Failure(validationResult.Error!);
		}

		ct.ThrowIfCancellationRequested();

		var employee = await repository.Query<FlowUser>()
			.Include(fu => fu.EmployeeUser)
				.ThenInclude(eu => eu.Address)
			.ByUserID(employeeId)
			.FirstOrDefaultAsync(ct);

		if (employee == null || employee.EmployeeUser == null)
		{
			return Result<EmployeeResponseDto>.Failure(ErrorMessages.UserNotFound, ResultErrorType.NotFound);
		}

		var currentEmail = employee.Email;

		employee.Email = !string.IsNullOrEmpty(request.Email) ? request.Email : employee.Email;
		employee.FirstName = !string.IsNullOrEmpty(request.FirstName) ? request.FirstName : employee.FirstName;
		employee.LastName = !string.IsNullOrEmpty(request.LastName) ? request.LastName : employee.LastName;
		employee.PhoneNumber = !string.IsNullOrEmpty(request.PhoneNumber) ? request.PhoneNumber : employee.PhoneNumber;

		var userRole = Enum.Parse<Roles>(request.Role);

		var assignRoleResult = await identityService.AssignUserToRole(employee.EmployeeUser.UserID, userRole, ct);

		if (!assignRoleResult.IsSuccess)
		{
			return Result<EmployeeResponseDto>.Failure(assignRoleResult.Error!, assignRoleResult.ErrorType);
		}

		employee.EmployeeUser.Role = request.Role;

		employee.EmployeeUser.Biography = request.Biography ?? request.Biography;
		employee.EmployeeUser.Memo = request.Memo ?? request.Memo;
		employee.EmployeeUser.DailyGoal = request.DailyGoal ?? request.DailyGoal;
		employee.EmployeeUser.BurdenRate = request.BurdenRate ?? request.BurdenRate;
		employee.EmployeeUser.Skills = request.Skills ?? request.Skills;
		employee.EmployeeUser.TwilioNumber = request.TwilioNumber ?? request.TwilioNumber;
		employee.EmployeeUser.TwilioCallerID = request.TwilioCallerID ?? request.TwilioCallerID;

		employee.IsDeleted = request.IsDeleted;
		employee.DateDeleted = request.IsDeleted ? DateTimeOffset.UtcNow : null;
		employee.DateUpdated = DateTimeOffset.UtcNow;

		var mapper = new EmployeeMapper();

		if (request.AddressLine1 != null &&
			request.City != null &&
			request.State != null &&
			request.ZipCode != null)
		{
			var state = await repository
				.Query<State>()
				.ByAbbreviation(request.State)
				.FirstOrDefaultAsync(ct);

			if (state == null)
			{
				return Result<EmployeeResponseDto>.Failure("State not found.", ResultErrorType.NotFound);
			}

			// Create and save the Address first
			employee.EmployeeUser.Address = mapper.CreateAddressFromEmployeeRequest(request, state);
		}

		if (request.Email != null &&
			!string.Equals(currentEmail, request.Email, StringComparison.OrdinalIgnoreCase))
		{
			var emailAlreadyInUse = await repository.Query<FlowUser>()
				.ByEmail(request.Email)
				.AnyAsync(ct);

			if (emailAlreadyInUse)
			{
				return Result<EmployeeResponseDto>.Failure("The provided email is already in use by another user.");
			}

			await identityRepository.UpdateUserEmail(employee.IdentityGuid.ToString(), request.Email, ct);
		}

		repository.AddOrUpdate(employee);
		await repository.SaveAsync(ct);

		var result = mapper.MapToEmployeeDtoFromEmployeeUser(employee.EmployeeUser);

		return Result<EmployeeResponseDto>.Success(result);
	}

	public async Task<Result<SearchResult<SearchEmployeeAndRoleDto>>> SearchEmployeesAndRoles(int pageIndex, int pageSize, string? keywordSearch, string? userRole, CancellationToken ct)
	{
		if (pageIndex < 0 || pageSize < 1)
		{
			return Result<SearchResult<SearchEmployeeAndRoleDto>>.Failure("Invalid pagination parameters.");
		}

		pageSize = Math.Min(pageSize, 50);

		var query = repository.Query<EmployeeUser>()
			.Include(x => x.FlowUser)
			.Include(x => x.Address)
			.OrderBy(x => x.FlowUser.DateCreated)
			.AsQueryable();

		if (!string.IsNullOrWhiteSpace(keywordSearch))
		{
			if (keywordSearch.Length < 2 || keywordSearch.Length > 100)
			{
				return Result<SearchResult<SearchEmployeeAndRoleDto>>.Failure(ErrorMessages.KeywordTooShort);
			}

			query = query
				.Where(eu =>
					EF.Functions.ILike(eu.FlowUser.FirstName, $"%{keywordSearch}%") ||
					EF.Functions.ILike(eu.FlowUser.LastName, $"%{keywordSearch}%") ||
					EF.Functions.ILike(eu.FlowUser.Email, $"%{keywordSearch}%"));
		}

		if (!string.IsNullOrWhiteSpace(userRole))
		{
			if (!Enum.TryParse<Roles>(userRole, out var parsedRole))
			{
				return Result<SearchResult<SearchEmployeeAndRoleDto>>.Failure("Invalid user role specified.");
			}

			query = query.ByRole(userRole);
		}

		ct.ThrowIfCancellationRequested();
		var totalRecordCount = await query.CountAsync(ct);

		ct.ThrowIfCancellationRequested();
		var users = await query
			.Skip(pageIndex * pageSize)
			.Take(pageSize)
			.AsNoTracking()
			.ToListAsync(ct);

		var mapper = new EmployeeMapper();
		var result = new SearchResult<SearchEmployeeAndRoleDto>(users.Select(mapper.Map), totalRecordCount);

		return Result<SearchResult<SearchEmployeeAndRoleDto>>.Success(result);
	}

	public async Task<Result<EmployeeResponseDto>> GetEmployeeDetailsByUserId(int userID, CancellationToken ct)
	{
		var mapper = new EmployeeMapper();

		ct.ThrowIfCancellationRequested();

		var user = await repository.Query<EmployeeUser>()
			.Include(x => x.FlowUser)
			.Include(x => x.Address)
			.ThenInclude(a => a.State)
			.ByUserID(userID)
			.FirstOrDefaultAsync(ct);

		if (user == null)
			return Result<EmployeeResponseDto>.Failure("Employee user not found", ResultErrorType.NotFound);

		var result = mapper.MapToEmployeeDtoFromEmployeeUser(user);

		return Result<EmployeeResponseDto>.Success(result);
	}
}
