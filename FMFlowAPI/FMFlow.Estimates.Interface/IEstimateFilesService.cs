using FMFlow.Estimates.Interface.DTOs;
using FMFlow.Files.Interface.DTOs;
using FMFlow.FlowAPI.Interface;

namespace FMFlow.Estimates.Interface;

public interface IEstimateFilesService
{
	Task<Result<List<FileResponseDto>>> GetFiles(int estimateID, bool includeSignatures, CancellationToken ct);
	Task<Result<FileDownloadResultDto>> DownloadFile(int estimateID, int fileID, bool shouldGetThumbnail, CancellationToken ct);
	Task<Result<FileUploadResultDto>> CreateEstimateFile(int estimateID, EstimateFileUploadRequestDto request, CancellationToken ct);
	Task<Result> DeleteEstimateFile(int estimateID, int fileID, CancellationToken ct);
}
