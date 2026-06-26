using FMFlow.Admin.Interface.DTOs;
using FMFlow.FlowAPI.Interface;

namespace FMFlow.Admin.Interface;

public interface ISheensService
{
	Task<Result<IEnumerable<SheenResponseDto>>> GetSheens(bool includeDeleted, CancellationToken ct);

	Task<Result<SheenResponseDto>> CreateSheen(SheenRequestDto request, CancellationToken ct);

	Task<Result<SheenResponseDto>> UpdateSheen(int sheenId, SheenRequestDto sheen, CancellationToken ct);

	Task<Result> UpdateSheenDeletedStatus(int sheenId, bool isDeleted, CancellationToken ct);

}
