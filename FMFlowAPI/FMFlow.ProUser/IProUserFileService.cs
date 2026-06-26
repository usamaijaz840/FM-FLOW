using FMFlow.Files.Interface.DTOs;
using FMFlow.FlowAPI.Interface;
using FMFlow.ProUser.Interface.DTOs;

namespace FMFlow.ProUser.Interface;

public interface IProUserFileService
{
	Task<Result<List<ProUserFileDto>>> GetFiles(int proUserId, CancellationToken ct);
	Task<Result<FileDownloadResultDto>> DownloadFile(int proUserId, int fileId, bool shouldGetThumbnail, CancellationToken ct);
	Task<Result<FileUploadResultDto>> CreateFile(int proUserId, ProUserFileUploadRequestDto request, CancellationToken ct);
	Task<Result> DeleteFile(int proUserId, int fileId, CancellationToken ct);
}
