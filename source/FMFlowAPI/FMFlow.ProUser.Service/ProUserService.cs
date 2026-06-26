using EFRepository;
using FluentValidation;
using FMFlow.Common;
using FMFlow.Common.ReCaptcha;
using FMFlow.Email.Interface;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.FlowAPI.Interface.Exceptions;
using FMFlow.Identity;
using FMFlow.Identity.Interface;
using FMFlow.Pro.Interface.Dtos;
using FMFlow.ProUser.Interface;
using FMFlow.ProUser.Interface.DTOs;
using FMFlow.ProUser.Mappers;
using FMFlow.ProUser.Service.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace FMFlow.ProUser.Service;

public class ProUserService(
	IIdentityService identityService,
	IIdentityRepository identityRepository,
	IRepository repository,
	IMemoryCache cache,
	IValidator<ProUserDto> validator,
	IReCaptchaService reCaptchaService,
	ICurrentUserService currentUserService,
	IEmailSenderService emailSenderService,
	ICustomJwtService customJwtService,
	IOptions<CustomJwtConfiguration> customJwtOptions) : IProUserService
{
	private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(1);
	private readonly CustomJwtConfiguration _customJwtConfig = customJwtOptions.Value;

	public async Task<Result<ProUserWithTokenDto>> CreateProWithToken(ProUserDto request, CancellationToken ct)
	{
		var requestValidation = await DtoValidator.Validate(request, validator, ct);

		if (!requestValidation.IsSuccess)
			return Result<ProUserWithTokenDto>.Failure(requestValidation.Error!);

		// Verify reCAPTCHA token
		//if (!string.IsNullOrWhiteSpace(request.ReCaptchaToken))
		//{
		//	bool reCaptchaSuccessful = await reCaptchaService.VerifyTokenIsValid(request.ReCaptchaToken, ct);
		//	if (reCaptchaSuccessful is false)
		//		return Result<ProUserDto>.Failure("Invalid reCaptcha challenge");
		//}

		var mapper = new ProUserMapper();
		var flowUser = mapper.MapToFlowUser(request);

		var createUserResult = await identityService.CreateUser(flowUser, ct);

		if (!createUserResult.IsSuccess)
		{
			return Result<ProUserWithTokenDto>.Failure(createUserResult.Error!);
		}

		var newProUser = mapper.MapToProUserDetail(request);
		newProUser.UserID = createUserResult.Value;
		newProUser.OnboardingFormStop = request.OnboardingFormStop;
		newProUser.DateCreated = DateTime.UtcNow;

		// Normalize optional state fields: store null instead of empty to satisfy FK
		if (newProUser.BusinessAddress != null
				&& string.IsNullOrWhiteSpace(newProUser.BusinessAddress?.StateId))
		{
			newProUser.BusinessAddress!.StateId = null;
		}
		if (newProUser.SherwinHomeAddress != null
			&& string.IsNullOrWhiteSpace(newProUser.SherwinHomeAddress?.StateId))
		{
			newProUser.SherwinHomeAddress!.StateId = null;
		}

		var timeZones = await GetTimeZones(ct);

		var timeZoneMatch = timeZones.FirstOrDefault(tz => tz.Name == request.TimeZone);

		if (timeZoneMatch == null)
			return Result<ProUserWithTokenDto>.Failure("Timezone does not exist");

		newProUser.FMTimeZoneID = timeZoneMatch.TimeZoneId;

		repository.AddNew(newProUser);

		await repository.SaveAsync(ct);

		var assignRoleResult = await identityService.AssignUserToRole(newProUser.UserID, Roles.Pro, ct);

		if (!assignRoleResult.IsSuccess)
		{
			return Result<ProUserWithTokenDto>.Failure(assignRoleResult.Error!, assignRoleResult.ErrorType);
		}

		var newProDto = mapper.MapToProUserDTO(newProUser);

		// Generate custom JWT for the new pro user
		var claims = new[] {
			new Claim(CustomClaimTypes.ExternalId, newProUser.UserID.ToString()),
			new Claim(CustomClaimTypes.PreferredUsername, flowUser.Email),
			new Claim(ClaimTypes.Role, nameof(Roles.Pro))
		};

		Result<string> tokenResult = customJwtService.GenerateCustomJwt(
			claims, _customJwtConfig.ProOnboardingTokenExpirationMinutes);

		if (!tokenResult.IsSuccess)
		{
			return Result<ProUserWithTokenDto>.Failure(tokenResult.Error!);
		}

		var response = new ProUserWithTokenDto(newProDto, tokenResult.Value!);

		return Result<ProUserWithTokenDto>.Success(response);
	}

	public async Task<Result<SearchResult<BasicProResponseDto>>> SearchPros(
		string? keywordSearch,
		CancellationToken ct,
		string? zipCode = null,
		bool? isApproved = null,
		int? projectId = null,
		int pageIndex = 0,
		int pageSize = 10,
		bool includeDeletedUsers = false)
	{
		if (pageIndex < 0 || pageSize < 1)
		{
			return Result<SearchResult<BasicProResponseDto>>.Failure("Invalid pagination parameters.");
		}

		pageSize = Math.Min(pageSize, 50);

		var query = repository.Query<FlowUser>()
			.Include(x => x.ProUser)
				.ThenInclude(p => p.BusinessAddress)
			.Where(u => u.ProUser != null && (includeDeletedUsers || u.IsDeleted == false));

		if (!string.IsNullOrWhiteSpace(keywordSearch))
		{
			if (keywordSearch.Length < 2 || keywordSearch.Length > 100)
			{
				return Result<SearchResult<BasicProResponseDto>>.Failure(ErrorMessages.KeywordTooShort);
			}

			query = query.Where(u =>
				EF.Functions.ILike(u.FirstName, $"%{keywordSearch}%") ||
				EF.Functions.ILike(u.LastName, $"%{keywordSearch}%") ||
				EF.Functions.ILike(u.Email, $"%{keywordSearch}%"));
		}

		if (!string.IsNullOrWhiteSpace(zipCode))
		{
			query = query.Where(u => u.ProUser!.BusinessAddress.ZipCode == zipCode);
		}

		if (isApproved.HasValue)
		{
			query = query.Where(u => u.ProUser!.IsApproved == isApproved.Value);
		}

		// Add account manager zip code filtering
		if (currentUserService.IsAccountManager())
		{
			var userId = currentUserService.GetUserID();

			ct.ThrowIfCancellationRequested();

			var zipCodesForCurrentAccountManager = await repository
				.Query<ZipCode>()
				.Where(z => z.EmployeesAssigned.Any(ea => ea.UserID == userId))
				.Select(p => p.Zipcode)
				.ToListAsync(ct);

			// Filter pros to only those who have zip codes that overlap with account manager's assigned zip codes
			query = query.Where(u => repository.Query<ProUserToProZipcode>()
				.Where(pz => pz.UserID == u.UserID && zipCodesForCurrentAccountManager.Contains(pz.Zipcode))
				.Any());
		}

		if (projectId.HasValue)
		{
			ct.ThrowIfCancellationRequested();

			var projectZipCode = await repository.Query<Project>()
				.AsNoTracking()
				.Where(p => p.ProjectID == projectId.Value)
				.Select(p => p.Address.ZipCode)
				.FirstOrDefaultAsync(ct);

			if (string.IsNullOrWhiteSpace(projectZipCode))
			{
				return Result<SearchResult<BasicProResponseDto>>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);
			}

			query = query.Where(pro => repository.Query<ProUserToProZipcode>()
				.Where(pz => pz.UserID == pro.UserID && pz.Zipcode == projectZipCode)
				.Any())
				.Where(u => u.ProUser != null && u.ProUser.RequestedReferrals == true && u.ProUser.IsApproved == true);
		}

		ct.ThrowIfCancellationRequested();

		var totalResults = await query.CountAsync(ct);

		ct.ThrowIfCancellationRequested();

		IOrderedQueryable<FlowUser> orderedQuery = query.OrderBy(u => u.ProUser!.IsApproved);

		if (projectId.HasValue)
		{
			orderedQuery = orderedQuery
				.ThenByDescending(u => u.ProUser!.DateAssignedToRsLead == null) // pros that were never assigned to an RS lead come first
				.ThenBy(u => u.ProUser!.DateAssignedToRsLead); // ascending = oldest referral source assignment to latest
		}

		// Within the above groupings, show newest users first
		orderedQuery = orderedQuery.ThenByDescending(u => u.DateCreated);

		var results = await orderedQuery
			.Skip(pageIndex * pageSize)
			.Take(pageSize)
			.ToListAsync(ct)!;

		Dictionary<int, List<string>> zipCodesPendingAssignment = [];
		var isSuperAdmin = currentUserService.IsSuperAdmin();

		if (isSuperAdmin)
		{
			zipCodesPendingAssignment = await GetZipCodesPendingAssignment(results, ct);
		}

		var mapper = new ProUserMapper();

		var searchResult = new SearchResult<BasicProResponseDto>(
			results.Select(user =>
			{
				var proZipCodesPendingAssignment = isSuperAdmin && zipCodesPendingAssignment.TryGetValue(user.UserID!.Value, out var pending)
					? pending : [];

				var dto = mapper.MapToBasicProDto(user, proZipCodesPendingAssignment);
				return dto;
			})
			.OrderBy(dto => dto.ZipCodesPendingAMAssignment == null || dto.ZipCodesPendingAMAssignment.Count == 0),
			totalResults);

		return Result<SearchResult<BasicProResponseDto>>.Success(searchResult);
	}

	private async Task<Dictionary<int, List<string>>> GetZipCodesPendingAssignment(List<FlowUser> results, CancellationToken ct)
	{
		var userIds = results.Select(u => u.UserID!.Value).ToList();

		ct.ThrowIfCancellationRequested();

		var proUserZipMappings = await repository.Query<ProUserToProZipcode>()
			.AsNoTracking()
			.ByIsDeleted(false)
			.Where(x => userIds.Contains(x.UserID))
			.ToListAsync(ct);

		ct.ThrowIfCancellationRequested();

		var zipCodesWithNoAMs = await repository.Query<ZipCode>()
			.AsNoTracking()
			.Where(z => !z.EmployeesAssigned.Any())
			.OrderByDescending(z => z.Zipcode)
			.Select(z => z.Zipcode)
			.ToListAsync(ct);

		var zipCodePendingAssingment = proUserZipMappings
			.Where(x => zipCodesWithNoAMs.Contains(x.Zipcode))
			.GroupBy(x => x.UserID)
			.ToDictionary(g => g.Key, g => g.Select(x => x.Zipcode).ToList());

		return zipCodePendingAssingment;
	}

	public async Task SavePaymentInfo(PaymentInfoModel info, string onboardingFormStop, CancellationToken ct)
	{
		var foundUser = await GetProUserByUserId(info.UserID, ct: ct);

		if (foundUser == null) return;

		repository.AddNew(info);

		await repository.SaveAsync(ct);

		await UpdateFormStop(info.UserID, onboardingFormStop, ct);
	}

	public IQueryable<ProUserDetail> GetActiveProUsers(bool? includeDeletedUsers = false)
	{
		var query = repository.Query<ProUserDetail>()
			.Include(x => x.FlowUser)
			.Include(x => x.FMTimeZone)
			.AsQueryable();

		if (!includeDeletedUsers.HasValue || !includeDeletedUsers.Value)
		{
			query = query.Where(x => x.FlowUser.IsDeleted == false);
		}

		return query;
	}

	public async Task<Result<ProUserDto>> GetProDetails(int proId, CancellationToken ct, bool? includeDeletedUsers = null, bool? willBeUpdating = null)
	{
		var user = await GetProUserByUserId(proId, includeDeletedUsers, ct);

		if (user == null)
		{
			return Result<ProUserDto>.Failure(ErrorMessages.UserNotFound, ResultErrorType.NotFound);
		}

		if (proId != currentUserService.GetUserID() && currentUserService.IsPro())
		{
			return Result<ProUserDto>.Failure("Pro users can only access their own details.", ResultErrorType.PermissionDenied);
		}

		var mapper = new ProUserMapper();
		var userDto = mapper.MapToProUserDTO(user);

		return Result<ProUserDto>.Success(userDto);
	}

	public async Task UpdateFormStop(int proId, string onboardingFormStop, CancellationToken ct)
	{
		var user = await GetProUserByUserId(proId, ct: ct);

		if (user == null)
		{
			throw new RecordNotFoundException("userID does not exist");
		}

		await UpdateFormStop(user, onboardingFormStop, true, ct);
	}

	public async Task UpdateFormStop(ProUserDetail user, string onboardingFormStop, bool shouldDoUpdateCall, CancellationToken ct)
	{
		user.OnboardingFormStop = onboardingFormStop;

		if (shouldDoUpdateCall)
		{
			repository.AddOrUpdate(user);
			await repository.SaveAsync(ct);
		}
	}

	public async Task<string?> GetUserOnboardingFormStop(int proId, CancellationToken ct)
	{
		var user = await GetProUserByUserId(proId, false, ct);

		if (user == null)
		{
			throw new RecordNotFoundException("userID does not exist");
		}

		return user.OnboardingFormStop;
	}

	public async Task<Result<ProUserDto>> UpdatePro(int proId, ProUserDto request, CancellationToken ct)
	{
		if (proId <= 0)
		{
			return Result<ProUserDto>.Failure("UserID is required and must be greater than 0.");
		}

		var existingUser = await GetProUserByUserId(proId, true, ct);

		if (existingUser == null)
		{
			return Result<ProUserDto>.Failure(ErrorMessages.UserNotFound);
		}

		// Only allow Pros to update themselves
		if (currentUserService.IsPro() && currentUserService.GetUserID() != proId)
		{
			return Result<ProUserDto>.Failure("Pros can only update their own profile.", ResultErrorType.PermissionDenied);
		}

		var mapper = new ProUserMapper();
		var stageMapper = new OnboardingStageMapper();
		var currentDto = mapper.MapToProUserDTO(existingUser);

		stageMapper.MergeOnboardingStageProperties(request, currentDto);

		currentDto.OnboardingFormStop = request.OnboardingFormStop;

		var requestValidation = await DtoValidator.Validate(currentDto, validator, ct);

		if (!requestValidation.IsSuccess)
		{
			return Result<ProUserDto>.Failure(requestValidation.Error!);
		}

		var currentEmail = existingUser.FlowUser!.Email;

		mapper.MapToProUserDetail(currentDto, existingUser);
		existingUser.DateUpdated = DateTime.UtcNow;

		// Associate the pro with the closest store zip code
		if (request.ZipCodeOfStore is not null)
		{
			await AssignZipCodeToProUser(existingUser.UserID, [request.ZipCodeOfStore], ct);
		}
		repository.AddOrUpdate(existingUser);
		await repository.SaveAsync(ct);

		if (request.Email != null && request.Email != string.Empty &&
			!string.Equals(currentEmail, request.Email, StringComparison.OrdinalIgnoreCase))
		{
			await identityRepository.UpdateUserEmail(existingUser.FlowUser.IdentityGuid.ToString(), request.Email, ct);
		}

		if (request.OnboardingFormStop?.ToLower() == "completed")
		{
			await emailSenderService.SendEmailProOnboardingComplete(existingUser, ct);
		}

		return Result<ProUserDto>.Success(currentDto);
	}

	public async Task<List<ProZipcode>> GetProZipcodes(string state, string county, CancellationToken ct)
	{
		var mapper = new ProZipcodeMapper();

		var proZipcodes = await repository.Query<ZipCode>()
			.Include(x => x.State)
			.ByStateAbbreviation(state)
			.ByCounty(county)
			.AsNoTracking()
			.ToListAsync(ct);

		return [.. proZipcodes.Select(mapper.Map)];
	}

	public async Task<Result> AssignZipCodeToProUser(int proUserId, string[] zipcodes, CancellationToken ct)
	{
		var foundUser = await GetProUserByUserId(proUserId, false, ct);

		if (foundUser == null)
		{
			return Result.Failure("Pro user not found.", ResultErrorType.NotFound);
		}

		// Get a count of the zip codes passed in that match a zipcode in the database to verify they are valid
		var proZipcodesCount = await repository.Query<Entities.ZipCode>()
			.Where(x => zipcodes.Contains(x.Zipcode))
			.AsNoTracking()
			.CountAsync(ct);

		if (proZipcodesCount != zipcodes.Length)
		{
			return Result.Failure("One or more zipcodes were not found.", ResultErrorType.NotFound);
		}

		// Get the list of all assigned zip codes
		var proZipCodeObjects = await repository.Query<ProUserToProZipcode>()
			.ByUserID(proUserId)
			.ToListAsync(ct);

		var proZipCodesOnly = proZipCodeObjects.Where(x => zipcodes.Contains(x.Zipcode)).ToList();

		// Add new zip codes for the zip codes that are not assigned
		foreach (var zipCode in zipcodes)
		{
			var existingRecord = proZipCodeObjects.FirstOrDefault(x => x.Zipcode == zipCode);

			if (existingRecord != null) continue;

			var newRecord = new ProUserToProZipcode()
			{
				UserID = proUserId,
				Zipcode = zipCode,
				DateCreated = DateTime.UtcNow
			};

			repository.AddNew(newRecord);
		}

		await repository.SaveAsync(ct);

		return Result.Success();
	}

	public async Task<Result> DeleteZipCodeToProUser(int proId, string zipcode, CancellationToken ct)
	{
		var foundUser = await GetProUserByUserId(proId, true, ct);

		var proZipcode = await repository.Query<ProUserToProZipcode>()
			.ByUserID(proId)
			.ByZipcode(zipcode)
			.AsNoTracking()
			.FirstOrDefaultAsync(ct);

		if (foundUser == null)
		{
			return Result<ProZipCodeStateResponseDto>.Failure("Pro user not found.", ResultErrorType.NotFound);
		}

		if (proZipcode == null)
		{
			return Result<ProZipCodeStateResponseDto>.Failure("Zipcode not found.", ResultErrorType.NotFound);
		}

		proZipcode.IsDeleted = true;
		proZipcode.DateDeleted = DateTime.UtcNow;

		repository.Delete(proZipcode);

		await repository.SaveAsync(ct);

		return Result.Success();
	}

	public async Task<Result<ProZipCodeStateResponseDto>> GetProUserZipCodes(int proId, CancellationToken ct)
	{
		// If the current user is a Pro, they can only access their own zip codes
		if (currentUserService.IsPro())
		{
			var currentUser = await currentUserService.GetCurrentUser(ct);
			if (currentUser == null || currentUser.UserID != proId)
			{
				return Result<ProZipCodeStateResponseDto>.Failure("Pro users can only access their own zip codes.", ResultErrorType.PermissionDenied);
			}
		}

		var foundUser = await GetProUserByUserId(proId, true, ct);

		if (foundUser == null)
		{
			return Result<ProZipCodeStateResponseDto>.Failure("Pro user not found.", ResultErrorType.NotFound);
		}

		var mapper = new ProUserMapper();

		var results = await repository.Query<ProUserToProZipcode>()
			.AsNoTracking()
			.ByUserID(proId)
			.ByIsDeleted(false)
			.Include(p => p.ProZipcode.State)
			.Include(x => x.ProUserDetail)
			.OrderBy(x => x.ProZipcode.State.StateName)
			.ThenBy(x => x.ProZipcode.County)
			.ThenBy(x => x.ProZipcode.Zipcode)
			.ToListAsync(ct);

		var result = ProUserMapper.MapToStateWithCounties(results);

		return Result<ProZipCodeStateResponseDto>.Success(result);
	}

	public async Task<List<FMTimeZone>> GetTimeZones(CancellationToken ct)
	{
		return await cache.GetOrCreateAsync("TimeZones", async entry =>
		{
			entry.AbsoluteExpirationRelativeToNow = _cacheDuration;

			return await repository.Query<FMTimeZone>()
				.OrderBy(x => x.TimeZoneId)
				.AsNoTracking()
				.ToListAsync(ct) ?? [];
		}) ?? [];
	}

	public async Task<ProZipCodeStateResponseDto> GetStatesAndCounties(CancellationToken ct)
	{
		var mapper = new ProZipcodeMapper();
		var results = await repository.Query<ZipCode>()
			.AsNoTracking()
			.OrderBy(p => p.State)
			.ThenBy(p => p.County)
			.ToListAsync(ct) ?? [];

		return mapper.MapToStateWithCounties(results);
	}

	public async Task<List<State>> GetStates(CancellationToken ct)
	{
		var results = await repository.Query<State>()
			.AsNoTracking()
			.OrderBy(p => p.StateName)
			.ToListAsync(ct) ?? [];

		return results;
	}

	public async Task<IEnumerable<CountyDto>> GetCounties(string state, CancellationToken ct)
	{
		var counties = await repository.Query<ZipCode>()
			.AsNoTracking()
			.Where(x => x.StateAbbreviation == state)
			.Select(x => x.County)
			.Distinct()
			.OrderBy(c => c)
			.ToListAsync(ct);

		var mapper = new ProCountiesMapper();
		return counties.Select(mapper.MapToCountyDto);
	}

	private async Task<ProUserDetail?> GetProUserByUserId(int proId, bool? includeDeletedUsers = null, CancellationToken ct = default)
	{
		return await GetActiveProUsers(includeDeletedUsers)
			.ByUserID(proId)
			.Include(p => p.FlowUser)
			.Include(p => p.BusinessAddress)
			.Include(p => p.SherwinHomeAddress)
			.FirstOrDefaultAsync(ct);
	}
}
