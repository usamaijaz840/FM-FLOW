using FMFlow.Common;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Pro.Interface.Dtos;
using FMFlow.ProUser.Interface.DTOs;

namespace FMFlow.ProUser.Interface;

public interface IProUserService
{
	Task<Result<ProUserWithTokenDto>> CreateProWithToken(ProUserDto info, CancellationToken ct);

	Task<Result<SearchResult<BasicProResponseDto>>> SearchPros(
			string? keywordSearch,
			CancellationToken ct,
			string? zipCode = null,
			bool? isApproved = null,
			int? projectId = null,
			int pageIndex = 0,
			int pageSize = 10,
			bool includeDeletedUsers = false);

	Task<string?> GetUserOnboardingFormStop(int proId, CancellationToken ct);

	Task UpdateFormStop(int proId, string onboardingFormStop, CancellationToken ct);

	Task SavePaymentInfo(PaymentInfoModel info, string onboardingFormStop, CancellationToken ct);

	IQueryable<ProUserDetail?> GetActiveProUsers(bool? includeDeletedUsers = null);

	Task<Result<ProUserDto>> UpdatePro(int proId, ProUserDto info, CancellationToken ct);

	Task<Result<ProUserDto>> GetProDetails(int proId, CancellationToken ct, bool? includeDeletedUsers = null, bool? willBeUpdating = null);

	Task<List<ProZipcode>> GetProZipcodes(string state, string county, CancellationToken ct);

	Task<Result> AssignZipCodeToProUser(int proId, string[] zipcodes, CancellationToken ct);

	Task<Result<ProZipCodeStateResponseDto>> GetProUserZipCodes(int userID, CancellationToken ct);

	Task<ProZipCodeStateResponseDto> GetStatesAndCounties(CancellationToken ct);

	Task<List<FMTimeZone>> GetTimeZones(CancellationToken ct);

	Task<List<State>> GetStates(CancellationToken ct);

	Task<IEnumerable<CountyDto>> GetCounties(string state, CancellationToken ct);

	Task<Result> DeleteZipCodeToProUser(int proId, string zipcode, CancellationToken ct);
}
