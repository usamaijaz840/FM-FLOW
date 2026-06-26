using System.Text.Json;
using EFRepository;
using FMFlow.AccessValidation;
using FMFlow.Common;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;
using FMFlow.Identity.Interface;
using FMFlow.LeadTimelines.Interface;
using FMFlow.LeadTimelines.Interface.DTOs;
using FMFlow.LeadTimelines.Service.Mapper;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.LeadTimelines.Service;

public class LeadTimelineService(
	IRepository repository,
	ICurrentUserService currentUserService,
	IAccessValidator accessValidator) : ILeadTimelineService
{
	public async Task<Result> RecordLeadTimelineAsync(Lead lead, TimelineEventKey eventKey, CancellationToken ct)
	{
		var currentUser = await currentUserService.GetCurrentUser(ct);
		var newTimeline = new LeadTimeline { LeadId = lead.LeadID };

		ApplyTemplate(newTimeline, lead, eventKey, currentUser);

		return await SaveTimelineAsync(newTimeline, ct);
	}

	public async Task<Result> RecordLeadTimelineAsync(Lead lead, TimelineEventKey eventKey, int userId, CancellationToken ct)
	{
		// For scenarios where user is not authenticated (e.g., customer self-signup)
		var user = await repository.Query<FlowUser>()
			.Where(u => u.UserID == userId)
			.FirstOrDefaultAsync(ct);

		var newTimeline = new LeadTimeline { LeadId = lead.LeadID };

		ApplyTemplate(newTimeline, lead, eventKey, user, userId);

		return await SaveTimelineAsync(newTimeline, ct);
	}

	public async Task<Result> RecordLeadTimelineAsync(Estimate estimate, TimelineEventKey eventKey, CancellationToken ct, string? statusUpdateReason = null)
	{
		var currentUser = await currentUserService.GetCurrentUser(ct);
		var newTimeline = new LeadTimeline { LeadId = estimate.RequestedEstimate.Project.LeadID };

		ApplyTemplate(newTimeline, estimate, eventKey, currentUser, statusUpdateReason);

		return await SaveTimelineAsync(newTimeline, ct);
	}

	public async Task<Result> RecordLeadTimelineAsync(Job job, TimelineEventKey eventKey, CancellationToken ct, string? statusUpdateReason = null)
	{
		var currentUser = await currentUserService.GetCurrentUser(ct);
		var newTimeline = new LeadTimeline { LeadId = job.Estimate.RequestedEstimate.Project.LeadID };

		ApplyTemplate(newTimeline, job, eventKey, currentUser, statusUpdateReason);

		return await SaveTimelineAsync(newTimeline, ct);
	}

	private async Task<Result> SaveTimelineAsync(LeadTimeline leadTimeline, CancellationToken ct)
	{
		repository.AddNew(leadTimeline);
		await repository.SaveAsync(ct);
		return Result.Success();
	}

	private string GetUserFirstName(FlowUser? currentUser)
		=> currentUser?.FirstName ?? currentUserService.Email ?? "Unknown";

	private string GetUserLastName(FlowUser? currentUser)
		=> currentUser?.LastName ?? string.Empty;

	private void ApplyTemplate(LeadTimeline newTimeline, TimelineEventKey eventKey, int userId, object values)
	{
		var template = new LeadTimelineTemplate(eventKey);
		newTimeline.EventNameKey = template.EventKeyAsString;
		newTimeline.EventKey = template.EventDescriptionKeyAsString;
		newTimeline.UserId = userId;
		newTimeline.DateCreated = DateTime.UtcNow;
		newTimeline.EventParameters = JsonDocument.Parse(JsonSerializer.Serialize(values));
	}

	private void ApplyTemplate(LeadTimeline newTimeline, TimelineEventKey eventKey, object values)
	{
		ApplyTemplate(newTimeline, eventKey, currentUserService.GetUserID(), values);
	}

	private void ApplyTemplate(LeadTimeline newTimeline, Lead lead, TimelineEventKey eventKey, FlowUser? currentUser, int? userId = null)
	{
		var parameters = new
		{
			leadName = $"{lead.FirstName} {lead.LastName}",
			userIdFirstName = GetUserFirstName(currentUser),
			userIdLastName = GetUserLastName(currentUser)
		};

		if (userId.HasValue)
		{
			ApplyTemplate(newTimeline, eventKey, userId.Value, parameters);
		}
		else
		{
			ApplyTemplate(newTimeline, eventKey, parameters);
		}
	}

	private void ApplyTemplate(LeadTimeline newTimeline, Estimate estimate, TimelineEventKey eventKey, FlowUser? currentUser, string? statusUpdateReason)
	{
		var parameters = new
		{
			proFirstName = estimate.ProUser.FirstName,
			proLastName = estimate.ProUser.LastName,
			userIdFirstName = GetUserFirstName(currentUser),
			userIdLastName = GetUserLastName(currentUser),
			statusUpdateReason = statusUpdateReason ?? string.Empty
		};

		ApplyTemplate(newTimeline, eventKey, parameters);
	}

	private void ApplyTemplate(LeadTimeline newTimeline, Job job, TimelineEventKey eventKey, FlowUser? currentUser, string? statusUpdateReason)
	{
		var parameters = new
		{
			proFirstName = job.Estimate.ProUser.FirstName,
			proLastName = job.Estimate.ProUser.LastName,
			userIdFirstName = GetUserFirstName(currentUser),
			userIdLastName = GetUserLastName(currentUser),
			statusUpdateReason = statusUpdateReason ?? string.Empty
		};

		ApplyTemplate(newTimeline, eventKey, parameters);
	}

	public async Task<Result<SearchResult<LeadTimelineResponseDto>>> GetLeadTimeline(int leadId, int pageIndex, int pageSize, CancellationToken ct)
	{
		var lead = await repository.Query<Lead>()
			.ByLeadID(leadId)
			.Include(l => l.Address)
			.FirstOrDefaultAsync(ct);

		if (lead == null)
			return Result<SearchResult<LeadTimelineResponseDto>>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToLead(lead, ct);

		if (!accessResult.IsSuccess)
			return Result<SearchResult<LeadTimelineResponseDto>>.Failure(accessResult.Error!, accessResult.ErrorType);

		var query = repository
			.Query<LeadTimeline>()
			.ByLeadId(leadId)
			.Include(lt => lt.User);

		var totalResults = await query.CountAsync(ct);

		var timelines = await query
			.OrderByDescending(tl => tl.DateCreated)
			.Skip(pageIndex * pageSize)
			.Take(pageSize)
			.ToListAsync(ct);

		var dtos = LeadTimelineMapper.MapToLeadTimelineResponseDtos(timelines);
		var searchResult = new SearchResult<LeadTimelineResponseDto>(dtos, totalResults);

		return Result<SearchResult<LeadTimelineResponseDto>>.Success(searchResult);
	}
}
