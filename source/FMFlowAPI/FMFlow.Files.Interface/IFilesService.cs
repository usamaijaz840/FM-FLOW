using FMFlow.Files.Interface.DTOs;
using FMFlow.FlowAPI.Interface;

namespace FMFlow.Files.Interface;

public interface IFilesService
{
	Task<Result> SoftDeleteFile(int fileID, CancellationToken ct);

	Task<Result> DeleteFile(int fileID, CancellationToken ct);

	Task<Result<FileDownloadResultDto>> GetFile(string key, CancellationToken ct);

	Task<Result<FileUploadResultDto>> UploadFileAsync(FileUploadRequestDto fileRequest, CancellationToken ct);

	Task<Result<ImageUploadResultDto>> UploadImageAsync(FileUploadRequestDto fileUpload, CancellationToken ct);
}
