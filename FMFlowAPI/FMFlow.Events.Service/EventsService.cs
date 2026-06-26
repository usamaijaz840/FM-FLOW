using Dapper;
using EFRepository;
using FMFlow.AccessValidation;
using FMFlow.Common;
using FMFlow.Data;
using FMFlow.Entities;
using FMFlow.Events.Interface.DTOs;
using FMFlow.FlowAPI.Interface;
using FMFlow.Identity.Interface;
using FMFlow.Integrations.Interface;
using FMFlow.Integrations.Interface.DTOs;
using FMFlow.ProUser.Interface;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FMFlow.Events.Service;

public class EventsService(
	IRepository repository,
	IEnumerable<ICalendarService> calendarServices,
	ICurrentUserService currentUserService,
	IAccessValidator accessValidator,
	ICustomerTempProsService customerTempProsService,
	ApplicationDbContext dbContext) : IEventsService
{
	public async Task<Result<ProEventsDto>> GetProEvents(
		DateOnly startDate,
		DateOnly endDate,
		int? proId,
		int? projectId,
		string? userTimeZone,
		CancellationToken ct,
		bool isMonthView = false,
		string? sessionId = null)
	{
		var parameters = new EventInputParameters(
			startDate,
			endDate,
			proId,
			projectId,
			userTimeZone,
			isMonthView,
			sessionId);

		return await ValidateGeneralInputs(parameters, ct)
			.MapResult(ValidateProInputs, ct)
			.MapResult(ValidateCustomerInputs, ct)
			.MapResult(ValidateAccountManagerInputs, ct)
			.MapResult(GetProInfo, ct)
			.MapResult(GetCalendarEvents, ct)
			.MapResult(GetFmFlowEvents, ct)
			.MapResult(MapEventsToAvailability, ct)
			.MapResult(ExtractResults, ct);
	}

	public async Task<Result<EventCalculationInputs>> ValidateGeneralInputs(EventInputParameters parameters, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		if (parameters.startDate > parameters.endDate)
		{
			return Result<EventCalculationInputs>.Failure("Start date cannot be on or after end date.", ResultErrorType.BadRequest);
		}

		int daysBetween = parameters.endDate.DayNumber - parameters.startDate.DayNumber;

		// The calendar on the front end displays up to 6 weeks at a time
		int maxDaysAllowedToRequest = 42;
		if (daysBetween > maxDaysAllowedToRequest)
		{
			return Result<EventCalculationInputs>.Failure($"The date range cannot exceed {maxDaysAllowedToRequest} days.", ResultErrorType.BadRequest);
		}

		TimeZoneInfo? userTimeZoneInfo = null;

		if (currentUserService.IsAccountManager() && !string.IsNullOrWhiteSpace(parameters.userTimeZone))
		{
			if (TimeZoneInfo.TryConvertIanaIdToWindowsId(parameters.userTimeZone!, out string? windowsTimeZoneId))
			{
				userTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(windowsTimeZoneId);
			}
			else
			{
				return Result<EventCalculationInputs>.Failure("The specified user time zone is not valid.", ResultErrorType.BadRequest);
			}
		}

		var result = new EventCalculationInputs(
			parameters.startDate,
			parameters.endDate,
			null,
			null,
			parameters.proId,
			parameters.projectId,
			null,
			userTimeZoneInfo,
			parameters.isMonthView,
			parameters.sessionId);

		if (parameters.projectId is not null)
        {
            return await accessValidator.ValidateAccessToProject(parameters.projectId.Value, ct)
				.MapResult(() => Result<EventCalculationInputs>.Success(result), ct);
        }

		return Result<EventCalculationInputs>.Success(result);
	}

	public async Task<Result<EventCalculationInputs>> ValidateProInputs(EventCalculationInputs parameters, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		if (!currentUserService.IsPro())
		{
			return Result<EventCalculationInputs>.Success(parameters);
		}

		if (parameters.ProId is not null && parameters.ProId != currentUserService.GetUserID())
		{
			return Result<EventCalculationInputs>.Failure("Pro ID must match the current user's ID.", ResultErrorType.PermissionDenied);
		}

		return Result<EventCalculationInputs>.Success(parameters);
	}

	public async Task<Result<EventCalculationInputs>> ValidateCustomerInputs(EventCalculationInputs parameters, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		if (!currentUserService.IsCustomer() && !currentUserService.IsTempCustomer() && !currentUserService.IsChatBot())
		{
			return Result<EventCalculationInputs>.Success(parameters);
		}

		if (!parameters.ProjectId.HasValue)
		{
			return Result<EventCalculationInputs>.Failure("Project ID is required for customers.", ResultErrorType.BadRequest);
		}

		if (parameters.ProId is not null)
		{
			if (!currentUserService.IsChatBot())
			{
				int? actualProId = await customerTempProsService.GetProId(parameters.ProId.Value, ct);
				if (actualProId is null)
				{
					return Result<EventCalculationInputs>.Failure("Pro ID not found.", ResultErrorType.NotFound);
				}

				parameters = parameters with
				{
					TempProId = parameters.ProId,
					ProId = actualProId.Value
				};
			}


			var proZipCodes = repository.Query<ProUserToProZipcode>()
					.ByUserID(parameters.ProId)
					.Select(p => p.Zipcode);

			string? projectZipCode = await repository.Query<Project>()
				.Include(p => p.Address)
				.ByProjectID(parameters.ProjectId.Value)
				.Select(p => p.Address.ZipCode)
				.FirstOrDefaultAsync(ct);

			if (!await proZipCodes.AnyAsync(z => z == projectZipCode, ct))
			{
				return Result<EventCalculationInputs>.Failure("The specified Pro ID does not service the area for the given project.", ResultErrorType.PermissionDenied);
			}
		}

		return Result<EventCalculationInputs>.Success(parameters);
	}

	public async Task<Result<EventCalculationInputs>> ValidateAccountManagerInputs(EventCalculationInputs parameters, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		if (!currentUserService.IsAccountManager())
		{
			return Result<EventCalculationInputs>.Success(parameters);
		}

		if (parameters.UserTimeZone is null)
		{
			return Result<EventCalculationInputs>.Failure("A user time zone is required for account managers.", ResultErrorType.BadRequest);
		}

		if (parameters.ProId is null)
		{
			return Result<EventCalculationInputs>.Failure("A pro ID is required for account managers", ResultErrorType.BadRequest);
		}

		var proZipCodes = repository.Query<ProUserToProZipcode>()
			.ByUserID(parameters.ProId)
			.Select(p => p.Zipcode);

		var userZipCodes = repository.Query<EmployeeUser>()
			.Include(e => e.AssignedZipCodes)
			.ByUserID(currentUserService.GetUserID())
			.SelectMany(e => e.AssignedZipCodes)
			.Where(z => proZipCodes.Contains(z.Zipcode));

		if (!await userZipCodes.AnyAsync(ct))
		{
			return Result<EventCalculationInputs>.Failure("You do not have access to the specified Pro ID.", ResultErrorType.PermissionDenied);
		}

		return Result<EventCalculationInputs>.Success(parameters);
	}

	public async Task<Result<EventCalculationResults>> GetProInfo(EventCalculationInputs parameters, CancellationToken ct)
	{
		int? currentProId = parameters.ProId;
		int? tempProId = null;

		if (parameters.ProId is null)
		{
			if (currentUserService.IsPro())
			{
				currentProId = currentUserService.GetUserID();
			}
			else if (currentUserService.IsCustomer() || currentUserService.IsTempCustomer() && parameters.ProjectId is not null)
			{
				string sql = $"""
					WITH temp_pro_ids AS
					(
					-- Find pros that have temp IDs for the current customer (so ones that have been looked at already by the customer)
					SELECT
					   ctp."ProId"
					  ,MAX(ctp."CustomerTempProId") AS "CustomerTempProId"
					FROM
					  "Projects" p
					  JOIN "Leads" l ON p."LeadID" = l."LeadID"
					  JOIN "CustomerTempPros" ctp ON l."CustomerID" = ctp."CustomerId"
					WHERE
					  p."ProjectID" = :projectId
					  AND ctp."ExpireDateTime" > NOW()
					GROUP BY
					  ctp."ProId"
					)
					SELECT
					   fu."UserID" AS "NextProId"
					  ,tpi."CustomerTempProId"
					FROM
					  "FMFlowUsers" fu
					  JOIN "ProUserDetails" pud ON fu."UserID" = pud."UserID"
					  LEFT JOIN temp_pro_ids tpi ON fu."UserID" = tpi."ProId"
					WHERE
					  fu."IsDeleted" = FALSE
					  AND pud."RequestedReferrals" = TRUE
					  AND pud."IsApproved" = TRUE
					  -- Make sure the pro isn't already assigned to this project.
					  AND NOT EXISTS (
					    SELECT
					      1
					    FROM
					      "RequestedEstimates" re
					      JOIN "Estimates" e ON re."RequestedEstimateID" = e."RequestedEstimateID"
					    WHERE
					      re."ProjectID" = :projectId
					      AND e."ProUserID" = fu."UserID")
					  -- Make sure the pro has a matching zip code for the project.
					  AND EXISTS (
					    SELECT
					      1
					    FROM
					      "Projects" cp
					      JOIN "Addresses" a ON cp."AddressID" = a."AddressID"
					      JOIN "ProUserToProZipcodes" ptpz ON ptpz."Zipcode" = a."ZipCode"
					    WHERE
					      cp."ProjectID" = :projectId
					      AND ptpz."UserID" = fu."UserID")
					ORDER BY
					   -- Order pros that don't have a temp ID to the top
					   (CASE WHEN tpi."ProId" IS NULL THEN 0 ELSE 1 END) ASC
					   -- Order pros to the top that were assigned a referral source lead the longest ago
					   -- to_timestamp(0) is used as default in case of null
					  ,COALESCE(pud."DateAssignedToRsLead", to_timestamp(0)) ASC;
					""";

				using var connection = new NpgsqlConnection(dbContext.Database.GetConnectionString());
				CustomerProSearchResult? nextPro = (await connection.QueryAsync<CustomerProSearchResult>(sql, new { projectId = parameters.ProjectId!.Value }))
					.FirstOrDefault();

				if (nextPro is not null)
				{
					currentProId = nextPro.NextProId;

					if (nextPro.CustomerTempProId is not null)
					{
						tempProId = nextPro.CustomerTempProId;
					}
					else
					{
						tempProId = await customerTempProsService.CreateTempProId(nextPro.NextProId, ct);
					}
				}
			}
			else if (currentUserService.IsChatBot() && parameters.ProjectId is not null)
			{
				if (parameters.SessionId is null)
				{
					parameters = parameters with
					{
						SessionId = Guid.NewGuid().ToString()
					};
				}

				string sql = $"""
					WITH session_pros AS
					(
					-- Find pros that have temp IDs for the current session (so ones that have been looked at already this session)
					SELECT
					   cbsp."ProId"
					FROM
					  "ChatBotSessionPros" cbsp
					WHERE
					  cbsp."SessionId" = :sessionId
					  AND cbsp."ExpireDateTime" > NOW()
					)
					SELECT
					  fu."UserID" AS "NextProId"
					FROM
					  "FMFlowUsers" fu
					  JOIN "ProUserDetails" pud ON fu."UserID" = pud."UserID"
					WHERE
					  fu."IsDeleted" = FALSE
					  AND pud."RequestedReferrals" = TRUE
					  AND pud."IsApproved" = TRUE
					  AND fu."UserID" NOT IN (SELECT "ProId" FROM session_pros)
					  -- Make sure the pro isn't already assigned to this project.
					  AND NOT EXISTS (
					    SELECT
					      1
					    FROM
					      "RequestedEstimates" re
					      JOIN "Estimates" e ON re."RequestedEstimateID" = e."RequestedEstimateID"
					    WHERE
					      re."ProjectID" = :projectId
					      AND e."ProUserID" = fu."UserID")
					  -- Make sure the pro has a matching zip code for the project.
					  AND EXISTS (
					    SELECT
					      1
					    FROM
					      "Projects" cp
					      JOIN "Addresses" a ON cp."AddressID" = a."AddressID"
					      JOIN "ProUserToProZipcodes" ptpz ON ptpz."Zipcode" = a."ZipCode"
					    WHERE
					      cp."ProjectID" = :projectId
					      AND ptpz."UserID" = fu."UserID")
					ORDER BY
					   -- Order pros to the top that were assigned a referral source lead the longest ago
					   -- to_timestamp(0) used as default in case of null
					  COALESCE(pud."DateAssignedToRsLead", to_timestamp(0)) ASC;
					""";

				using var connection = new NpgsqlConnection(dbContext.Database.GetConnectionString());

				int nextProId = (await connection.QueryAsync<int>(sql, new { projectId = parameters.ProjectId!.Value, sessionId = parameters.SessionId }))
					.FirstOrDefault();

				if (nextProId != 0)
				{
					currentProId = nextProId;

					var chatBotSessionPro = new ChatBotSessionPro
					{
						ProId = nextProId,
						SessionId = parameters.SessionId!
					};

					repository.AddNew(chatBotSessionPro);

					await repository.SaveAsync();
				}
			}
		}

		if (currentProId is null)
		{
			return Result<EventCalculationResults>.Failure("Pro not found.", ResultErrorType.NotFound);
		}

		FlowUser? currentPro = await repository.Query<FlowUser>()
			.Include(u => u.ProWeekDayAvailabilities)
			.Include(u => u.ProUser)
				.ThenInclude(pu => pu!.FMTimeZone)
			.Where(u => u.UserID == currentProId && u.ProUser != null)
			.FirstOrDefaultAsync(ct);

		if (currentPro?.ProUser?.FMTimeZone is null)
		{
			return Result<EventCalculationResults>.Failure("Pro not found.", ResultErrorType.NotFound);
		}

		var proInfo = new EventProInfo(
			currentPro.UserID!.Value,
			TimeZoneInfo.FindSystemTimeZoneById(currentPro.ProUser!.FMTimeZone!.SystemTimeZoneId),
			currentPro.ProWeekDayAvailabilities.ToDictionary(a => a.DayOfWeek));

		var targetStartDateTime = parameters.StartDate.ToDateTime(TimeOnly.MinValue);
		var targetEndDateTime = parameters.EndDate.AddDays(1).ToDateTime(TimeOnly.MinValue).AddTicks(-1);
		var startDateTime = new DateTimeOffset(targetStartDateTime, proInfo.TimeZone.GetUtcOffset(targetStartDateTime));
		var endDateTime = new DateTimeOffset(targetEndDateTime, proInfo.TimeZone.GetUtcOffset(targetEndDateTime));
		int? resultProId = currentUserService.IsChatBot() ? currentPro.UserID : null;

		return Result<EventCalculationResults>.Success(
			new EventCalculationResults(
				parameters with
				{
					StartDateTime = startDateTime,
					EndDateTime = endDateTime,
					TempProId = parameters.TempProId ?? tempProId
				},
				proInfo,
				new ProEventsDto([], null, resultProId, parameters.TempProId ?? tempProId, currentPro.ProUser!.FMTimeZone!.Name, parameters.SessionId)));
	}

	public async Task<Result<EventCalculationResults>> GetCalendarEvents(EventCalculationResults calculationResults, CancellationToken ct)
	{
		if (calculationResults.Parameters.IsMonthView)
		{
			// For month view, we only show FM Flow events to improve performance.
			return Result<EventCalculationResults>.Success(calculationResults);
		}

		IEnumerable<BusyTime>? busyTimes = null;

		foreach (var calendarService in calendarServices)
		{
			if (await calendarService.IsCalendarConnected(ct, calculationResults.ProInfo.ProId))
			{
				busyTimes = await calendarService.GetBusyTimes(calculationResults.Parameters.StartDateTime!.Value.UtcDateTime, calculationResults.Parameters.EndDateTime!.Value.UtcDateTime, ct, calculationResults.ProInfo.ProId);

				break;
			}
		}

		if (busyTimes is not null)
		{
			calculationResults.Events?.Events?.AddRange(busyTimes.Select(b =>
			{
				if (b.TimeZone is null || !TimeZoneInfo.TryFindSystemTimeZoneById(b.TimeZone, out TimeZoneInfo? timeZone))
				{
					timeZone = TimeZoneInfo.Utc;
				}

				return new EventDto(
					"Busy",
					"Personal Event",
					new DateTimeOffset(b.Start, timeZone.GetUtcOffset(b.Start)),
					new DateTimeOffset(b.End, timeZone.GetUtcOffset(b.End)),
					null,
					null,
					false,
					null,
					EventType.Personal,
					null,
					null,
					null
				);
			}));
		}

		return Result<EventCalculationResults>.Success(calculationResults);
	}

	public async Task<Result<EventCalculationResults>> GetFmFlowEvents(EventCalculationResults calculationResults, CancellationToken ct)
	{
		var scheduledEstimates = await repository.Query<ScheduledEstimate>()
			.Include(se => se.Project)
				.ThenInclude(p => p.Lead)
			.Include(se => se.Estimates.Where(e => e.IsActive && e.Status != EstimateStatus.Canceled))
				.ThenInclude(e => e.RequestedEstimate)
					.ThenInclude(re => re.EstimateType)
			.Include(se => se.Project)
				.ThenInclude(p => p.Address)
					.ThenInclude(se => se.State)
			.ByProUserID(calculationResults.ProInfo.ProId)
			.ByScheduledDateTimeBetween(
				calculationResults.Parameters.StartDateTime!.Value.ToUniversalTime(),
				calculationResults.Parameters.EndDateTime!.Value.ToUniversalTime())
			.Where(se => se.Estimates.Any(e => e.IsActive && e.Status != EstimateStatus.Canceled))
			.ToListAsync(ct);

		if (scheduledEstimates.Count != 0)
		{
			calculationResults.Events.Events?.AddRange(scheduledEstimates.Select(se =>
			{
				var projectName = se.Project.GetShortName();
				string leadName = se.Project.Lead.GetFullName();
				string status = ((EstimateStatus)se.Estimates.Select(e => (int)e.Status).Max()).ToString();
				bool isChangeOrder = se.Estimates.Select(e => e.RequestedEstimate.IsChangeOrder).Any(c => c);

				IEnumerable<EstimateType> estimateTypes = se.Estimates.Select(e => e.RequestedEstimate.EstimateType);

				bool hasInterior = estimateTypes.Any(x => x.EstimateTypeAbbreviation.Contains("Int"));
				bool hasExterior = estimateTypes.Any(x => x.EstimateTypeAbbreviation.Contains("Ext"));

				// Build the prefix based on interior/exterior
				string locationPrefix = (hasInterior, hasExterior) switch
				{
					(true, true) => "(Int./Ext.) ",
					(true, false) => "(Int.) ",
					(false, true) => "(Ext.) ",
					_ => "" // No areas found
				};

				// Build the title with the location prefix
				string title = $"{locationPrefix}{projectName}";

				// Build the description with estimate count and lead name
				int estimateCount = se.Estimates.Count;
				string estimateText = estimateCount > 1 ? $" ({estimateCount} estimates)" : string.Empty;
				string description = $"{leadName}{estimateText}";

				return new EventDto(
					title,
					description,
					se.ScheduledDateTime,
					se.ScheduledDateTime.AddMinutes(90),
					null,
					null,
					false,
					status,
					isChangeOrder ? EventType.ChangeOrder : EventType.Estimate,
					se.ProjectID,
					se.Project.Address.ToString(),
					se.ScheduledEstimateID);
			}));
		}

		var scheduledJobs = await repository.Query<Job>()
			.Include(j => j.Estimate)
				.ThenInclude(e => e.RequestedEstimate)
					.ThenInclude(re => re.Project)
						.ThenInclude(p => p.Lead)
			.Include(j => j.Estimate)
				.ThenInclude(e => e.RequestedEstimate)
					.ThenInclude(re => re.Project)
			.Include(j => j.Estimate)
				.ThenInclude(e => e.RequestedEstimate)
					.ThenInclude(re => re.Project)
						.ThenInclude(p => p.Address)
							.ThenInclude(se => se.State)
			.Include(j => j.Estimate)
				.ThenInclude(e => e.RequestedEstimate)
					.ThenInclude(re => re.EstimateType)
			.Where(j => j.Estimate.ProUserID == calculationResults.ProInfo.ProId)
			.ByScheduledDateWorkStartedIsBefore(calculationResults.Parameters.EndDate.ToDateTime(TimeOnly.MaxValue).ToUniversalTime())
			.ByScheduledDateWorkCompletedGreaterThanOrEqual(calculationResults.Parameters.StartDate)
			.ByIsActive(true)
			.ToListAsync(ct);

		if (scheduledJobs.Count != 0)
		{
			calculationResults.Events.Events?.AddRange(scheduledJobs.Select(j =>
			{
				string projectName = j.Estimate.RequestedEstimate.Project.GetShortName();
				string leadName = j.Estimate.RequestedEstimate.Project.Lead.GetFullName();

				string prefix = $"({j.Estimate.RequestedEstimate.EstimateType.EstimateTypeAbbreviation}.) ";
				string eventName = $"{prefix}{projectName}";

				// Create all-day events for jobs
				return new EventDto(
					eventName,
					leadName,
					null,
					null,
					DateOnly.FromDateTime(j.ScheduledDateWorkStarted.LocalDateTime),
					j.ScheduledDateWorkCompleted,
					true,
					j.Status.ToString(),
					EventType.Job,
					j.Estimate.RequestedEstimate.ProjectID,
					j.Estimate.RequestedEstimate.Project.Address.ToString(),
					j.JobId);
			}));
		}

		return Result<EventCalculationResults>.Success(calculationResults);
	}

	public async Task<Result<EventCalculationResults>> MapEventsToAvailability(EventCalculationResults calculationResults, CancellationToken ct)
	{
		const int AvailabilityBlockSize = 30; // Time in minutes between availability events
											  // The length of an estimate is 1 hour and 45 minutes
		const double PreEventBuffer = 1.50;
		const int MinuteBufferAfterEvent = 30;
		// In the case of a Pro user, we do not need the availability, so just return the events.
		if (currentUserService.IsPro())
		{
			return Result<EventCalculationResults>.Success(calculationResults);
		}

		// These are to collect the unavailable times and dates based on the events.
		HashSet<DateTimeOffset> unavailableTimes = [];
		HashSet<DateOnly> unavailableDates = [];

		if (calculationResults.Events?.Events is not null)
		{
			foreach (var ev in calculationResults.Events.Events)
			{
				// All day events add unavailable dates
				if (ev.IsAllDay && ev.StartDate is not null && ev.EndDate is not null)
				{
					var currentEventDate = ev.StartDate.Value;

					while (currentEventDate <= ev.EndDate)
					{
						unavailableDates.Add(currentEventDate);
						currentEventDate = currentEventDate.AddDays(1);
					}
				}
				// Other dates add unavailable times
				else if (ev.StartDateTime is not null && ev.EndDateTime is not null)
				{
					var roundedStartTime = RoundDown(ev.StartDateTime.Value, TimeSpan.FromMinutes(AvailabilityBlockSize));
					// Start blocking time AvailabilityBlockSize minutes before the LengthOfEventInHours
					var preBufferStartTime = roundedStartTime.AddHours(-PreEventBuffer);
					var currentUnavailableTimeOffset = RoundUpOffset(preBufferStartTime, TimeSpan.FromMinutes(AvailabilityBlockSize));
					var targetEndTime = ev.EndDateTime.Value.AddMinutes(MinuteBufferAfterEvent);

					while (currentUnavailableTimeOffset < targetEndTime)
					{
						unavailableTimes.Add(currentUnavailableTimeOffset.ToUniversalTime());
						currentUnavailableTimeOffset = currentUnavailableTimeOffset.AddMinutes(AvailabilityBlockSize);
					}
				}
			}
		}

		// Move back a day here so we can increment at the beginning of the loop below,
		// allowing us to use the continue statement to skip unavailable dates.
		var currentDate = calculationResults.Parameters.StartDate.AddDays(-1);

		List<DateTimeOffset> availableTimes = [];

		while (currentDate < calculationResults.Parameters.EndDate)
		{
			currentDate = currentDate.AddDays(1);

			if (unavailableDates.Contains(currentDate))
			{
				continue;
			}

			// If the pro doesn't have availability times declared, then we assume they are available all day.
			var startTime = currentDate.ToDateTime(TimeOnly.MinValue);
			var endTime = currentDate.AddDays(1).ToDateTime(TimeOnly.MinValue).AddTicks(-1);

			if (calculationResults.ProInfo.Availability.Count > 0)
			{
				if (calculationResults.ProInfo.Availability.TryGetValue(currentDate.DayOfWeek, out ProWeekDayAvailability? availability) && availability is not null)
				{
					// If availability is set for the day, then use the declared availability for the time range
					startTime = currentDate.ToDateTime(availability.StartTime);
					endTime = currentDate.ToDateTime(availability.EndTime);
				}
				else
				{
					// If no availability is set for the day, then skip to the next date
					continue;
				}
			}

			// Round the start time to the next block minute mark
			startTime = RoundUp(startTime, TimeSpan.FromMinutes(AvailabilityBlockSize));

			// Convert the time to a DateTimeOffset with the Pro's time zone offset so we can match against the event times
			var currentTime = new DateTimeOffset(startTime, calculationResults.ProInfo.TimeZone.GetUtcOffset(startTime));
			var endTimeWithOffset = new DateTimeOffset(endTime, calculationResults.ProInfo.TimeZone.GetUtcOffset(endTime));

			while (currentTime <= endTimeWithOffset)
			{
				if (!unavailableTimes.Contains(currentTime.ToUniversalTime()))
				{
					availableTimes.Add(currentTime);
				}

				currentTime = currentTime.AddMinutes(AvailabilityBlockSize);
			}
		}

		// If the user is a customer or temp customer, we do not return the events, only the available times.
		var returnedEvents = currentUserService.IsCustomer() || currentUserService.IsTempCustomer() || currentUserService.IsChatBot()
			? null
			: calculationResults.Events?.Events;

		var nextResults = calculationResults.Events! with
		{
			Events = returnedEvents,
			Availability = availableTimes
		};

		calculationResults = calculationResults with
		{
			Events = nextResults
		};

		return Result<EventCalculationResults>.Success(calculationResults);
	}

	public static async Task<Result<ProEventsDto>> ExtractResults(EventCalculationResults results, CancellationToken ct)
	{
		int eventCount = results.Events.Events?.Count ?? 0;
		int availabilityCount = results.Events.Availability?.Count ?? 0;

		if (results.Parameters.UserTimeZone is null)
		{
			return Result<ProEventsDto>.Success(results.Events);
		}

		var eventResults = results.Events;

		var impersonatedEvents = eventResults.Events?.Select(e =>
		{
			if (e.StartDateTime is null || e.EndDateTime is null)
			{
				return e;
			}

			var impersonatedStartTime = TimeZoneHelper.PreserveTimeInDifferentTimeZone(e.StartDateTime.Value, results.ProInfo.TimeZone, results.Parameters.UserTimeZone);
			var impersonatedEndTime = TimeZoneHelper.PreserveTimeInDifferentTimeZone(e.EndDateTime.Value, results.ProInfo.TimeZone, results.Parameters.UserTimeZone);

			return e with
			{
				StartDateTime = impersonatedStartTime,
				EndDateTime = impersonatedEndTime
			};
		});

		var impersonatedAvailability = eventResults.Availability?.Select(a => TimeZoneHelper.PreserveTimeInDifferentTimeZone(a, results.ProInfo.TimeZone, results.Parameters.UserTimeZone));

		return Result<ProEventsDto>.Success(eventResults with
		{
			Events = impersonatedEvents?.ToList(),
			Availability = impersonatedAvailability?.ToList()
		});

	}

	/// <summary>
	/// Rounds a DateTime up to the nearest specified time interval while preserving the DateTimeKind.
	/// </summary>
	/// <param name="dt">The DateTime to round up</param>
	/// <param name="d">The time interval to round up to (e.g., TimeSpan.FromMinutes(30) for 30-minute blocks)</param>
	/// <returns>A DateTime rounded up to the nearest interval with the original DateTimeKind preserved</returns>
	/// <exception cref="ArgumentException">Thrown when the interval is zero or negative</exception>
	private static DateTime RoundUp(DateTime dt, TimeSpan d)
	{
		if (d.Ticks <= 0)
		{
			throw new ArgumentException("Time interval must be positive and non-zero", nameof(d));
		}

		return new DateTime((dt.Ticks + d.Ticks - 1) / d.Ticks * d.Ticks, dt.Kind);
	}

	/// <summary>
	/// Rounds a DateTimeOffset down to the nearest specified time interval while preserving the timezone offset.
	/// </summary>
	/// <param name="dt">The DateTimeOffset to round down</param>
	/// <param name="d">The time interval to round down to (e.g., TimeSpan.FromMinutes(30) for 30-minute blocks)</param>
	/// <returns>A DateTimeOffset rounded down to the nearest interval with the original timezone offset preserved</returns>
	/// <exception cref="ArgumentException">Thrown when the interval is zero or negative</exception>
	private static DateTimeOffset RoundDown(DateTimeOffset dt, TimeSpan d)
	{
		if (d.Ticks <= 0)
			throw new ArgumentException("Time interval must be positive and non-zero", nameof(d));

		return new DateTimeOffset(dt.Ticks / d.Ticks * d.Ticks, dt.Offset);
	}

	/// <summary>
	/// Rounds a DateTimeOffset up to the nearest specified time interval while preserving the timezone offset.
	/// This method ensures cross-platform compatibility by maintaining DateTimeOffset throughout the calculation
	/// without converting to DateTime, avoiding platform-specific UTC conversion differences.
	/// </summary>
	/// <param name="dt">The DateTimeOffset to round up</param>
	/// <param name="d">The time interval to round up to (e.g., TimeSpan.FromMinutes(30) for 30-minute blocks)</param>
	/// <returns>A DateTimeOffset rounded up to the nearest interval with the original timezone offset preserved</returns>
	/// <exception cref="ArgumentException">Thrown when the interval is zero or negative</exception>
	private static DateTimeOffset RoundUpOffset(DateTimeOffset dt, TimeSpan d)
	{
		if (d.Ticks <= 0)
			throw new ArgumentException("Time interval must be positive and non-zero", nameof(d));

		return new DateTimeOffset((dt.Ticks + d.Ticks - 1) / d.Ticks * d.Ticks, dt.Offset);
	}
}

public record EventInputParameters(
	DateOnly startDate,
	DateOnly endDate,
	int? proId,
	int? projectId,
	string? userTimeZone,
	bool isMonthView = false,
	string? sessionId = null
);

public record EventCalculationInputs(
	DateOnly StartDate,
	DateOnly EndDate,
	DateTimeOffset? StartDateTime,
	DateTimeOffset? EndDateTime,
	int? ProId,
	int? ProjectId,
	int? TempProId,
	TimeZoneInfo? UserTimeZone,
	bool IsMonthView,
	string? SessionId = null
);

public record CustomerProSearchResult(
	int NextProId,
	int? CustomerTempProId);

public record EventProInfo(
	int ProId,
	TimeZoneInfo TimeZone,
	Dictionary<DayOfWeek, ProWeekDayAvailability> Availability
);

public record EventCalculationResults(
	EventCalculationInputs Parameters,
	EventProInfo ProInfo,
	ProEventsDto Events
);
