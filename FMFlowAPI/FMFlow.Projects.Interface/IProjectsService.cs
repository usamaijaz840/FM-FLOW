using FMFlow.Common;
using FMFlow.FlowAPI.Interface;
using FMFlow.Projects.Interface.DTOs;

namespace FMFlow.Projects.Interface;

public interface IProjectsService
{
	Task<Result<ProjectResponseDto>> CreateProject(ProjectRequestDto createProjectRequest, CancellationToken ct);

	Task<Result<ProjectResponseDto>> CreateCustomerProject(CustomerProjectRequestDto createProjectRequest, CancellationToken ct);

	Task<Result<SearchResult<ProjectResponseDto>>> SearchProjects(
		int? leadID,
		string? keywordSearch,
		bool? isOpen,
		bool? isDeleted,
		DateTimeOffset? lastUpdated,
		int? proUserID,
		string? zipCode,
		int pageIndex,
		int pageSize,
		CancellationToken ct);

	Task<Result<ProjectResponseDto>> UpdateProject(int projectID, ProjectUpdateRequestDto request, CancellationToken ct);

	Task<Result> DeleteProject(int projectID, CancellationToken ct);

	Task<Result> RestoreProject(int projectID, CancellationToken ct);

	Task<Result<DetailedProjectResponseDto>> GetProject(int projectID, CancellationToken ct);
}
