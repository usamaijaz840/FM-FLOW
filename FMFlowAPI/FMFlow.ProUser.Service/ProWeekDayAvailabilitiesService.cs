using EFRepository;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Identity.Interface;
using FMFlow.ProUser.Interface.DTOs;
using FMFlow.ProUser.Mappers;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.ProUser;

public class ProWeekDayAvailabilitiesService(
	IRepository repository,
	ICurrentUserService currentUserService) : IProWeekDayAvailabilitiesService
{
	public async Task<Result> UpdateWeekDayAvailabilities(int proUserId, WeekDayAvailabilityDTO[] availabilities, CancellationToken ct)
	{
		if (!currentUserService.IsPro())
			return Result.Failure(ErrorMessages.ResourceAccessDenied);

		if (currentUserService.GetUserID() != proUserId)
			return Result.Failure(ErrorMessages.ResourceAccessDenied);

		var mapper = new ProWeekDayAvailabilityMapper();
		var oldAvailabilities = await repository.Query<ProWeekDayAvailability>()
			.ByProUserID(proUserId)
			.AsNoTracking()
			.ToListAsync(ct);

		foreach (var old in oldAvailabilities)
		{
			repository.Delete(old);
		}

		foreach (var dto in availabilities)
		{
			var entity = mapper.MapToWeekDayAvailabilityDto(dto, proUserId);

			repository.AddNew(entity);
		}
		await repository.SaveAsync(ct);

		return Result.Success();
	}

	public async Task<List<ProWeekDayAvailability>> GetWeekDayAvailabilities(int proUserId, CancellationToken ct)
	{
		if (!currentUserService.IsPro())
			return [];

		if (currentUserService.GetUserID() != proUserId)
			return [];

		var results = await repository.Query<ProWeekDayAvailability>()
			.OrderBy(x => x.DayOfWeek)
			.AsNoTracking()
			.ByProUserID(proUserId)
			.ToListAsync(ct) ?? [];

		return results;
	}
}
