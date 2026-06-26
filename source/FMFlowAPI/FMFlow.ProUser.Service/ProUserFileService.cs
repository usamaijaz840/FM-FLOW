using EFRepository;
using FMFlow.Entities;
using FMFlow.Files.Interface;
using FMFlow.Files.Interface.DTOs;
using FMFlow.FlowAPI.Interface;
using FMFlow.ProUser.Interface;
using FMFlow.ProUser.Interface.DTOs;
using FMFlow.ProUser.Mappers;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.ProUser.Service;

public class ProUserFileService(
	IRepository repository,
	IFilesService filesService) : IProUserFileService
{
	public async Task<Result<List<ProUserFileDto>>> GetFiles(int proUserID, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		// For Insurance files, filter out soft-deleted ones. For other types, get all files.
		var proUserFiles = await repository
			.Query<ProUserFile>()
			.ByUserID(proUserID)
			.Include(x => x.File)
			.Include(x => x.ThumbnailFile)
			.Where(x => x.ProFileType != ProUserFileType.Insurance || !x.File.IsDeleted)
			.ToListAsync(ct);

		var mapper = new ProUserMapper();

		var fileRequests = proUserFiles.Select(ProUserMapper.MapToProUserFileDto).ToList();
		return Result<List<ProUserFileDto>>.Success(fileRequests);
	}

	public async Task<Result<FileDownloadResultDto>> DownloadFile(int proUserId, int fileId, bool shouldGetThumbnail, CancellationToken ct)
	{
		var proUserFileQuery = repository
			.Query<ProUserFile>()
			.ByUserID(proUserId)
			.ByFileID(fileId);

		var proUserFileExists = await proUserFileQuery
			.AnyAsync(ct);

		if (proUserFileExists == false)
			return Result<FileDownloadResultDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var fileKey = string.Empty;
		var fileName = string.Empty;

		ct.ThrowIfCancellationRequested();

		if (shouldGetThumbnail)
		{
			var proUserFile = await proUserFileQuery
									.Include(x => x.ThumbnailFile)
									.Select(x => new { x.ThumbnailFile, x.ProFileType })
									.FirstOrDefaultAsync(ct);

			if (proUserFile?.ThumbnailFile == null)
				return Result<FileDownloadResultDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

			// For Insurance files, check if soft deleted. For other types, allow access.
			if (proUserFile.ProFileType == ProUserFileType.Insurance && proUserFile.ThumbnailFile.IsDeleted)
				return Result<FileDownloadResultDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

			fileKey = proUserFile.ThumbnailFile.Key;
			fileName = proUserFile.ThumbnailFile.Name;
		}
		else
		{
			var proUserFile = await proUserFileQuery
									.Include(x => x.File)
									.Select(x => new { x.File, x.ProFileType })
									.FirstOrDefaultAsync(ct);

			if (proUserFile?.File == null)
				return Result<FileDownloadResultDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

			// For Insurance files, check if soft deleted. For other types, allow access.
			if (proUserFile.ProFileType == ProUserFileType.Insurance && proUserFile.File.IsDeleted)
				return Result<FileDownloadResultDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

			fileKey = proUserFile.File.Key;
			fileName = proUserFile.File.Name;
		}

		var fileResult = await filesService.GetFile(fileKey, ct);
		fileResult.Value!.FileName = fileName;

		return fileResult;
	}

	public async Task<Result<FileUploadResultDto>> CreateFile(int proUserId, ProUserFileUploadRequestDto request, CancellationToken ct)
	{
		// Handle existing files based on type
		if (request.ProFileType == ProUserFileType.Insurance)
		{
			// For Insurance files, use soft delete
			ct.ThrowIfCancellationRequested();
			var existingInsuranceFiles = await repository
				.Query<ProUserFile>()
				.Where(x => x.UserID == proUserId && x.ProFileType == ProUserFileType.Insurance)
				.Include(x => x.File)
				.Include(x => x.ThumbnailFile)
				.ToListAsync(ct);

			foreach (var existingFile in existingInsuranceFiles)
			{
				await filesService.SoftDeleteFile(existingFile.FileID, ct);
				if (existingFile.ThumbnailFileID != null)
					await filesService.SoftDeleteFile(existingFile.ThumbnailFileID.Value, ct);
			}
		}
		else
		{
			// For non-Insurance files, check if we need to replace existing files of the same type
			ct.ThrowIfCancellationRequested();
			var existingFiles = await repository
				.Query<ProUserFile>()
				.Where(x => x.UserID == proUserId && x.ProFileType == request.ProFileType)
				.Include(x => x.File)
				.Include(x => x.ThumbnailFile)
				.ToListAsync(ct);

			foreach (var existingFile in existingFiles)
			{
				// Delete the ProUserFile record first, before deleting the actual files
				repository.Delete(existingFile);
			}

			// Save the ProUserFile deletions first
			if (existingFiles.Any())
				await repository.SaveAsync(ct);

			// Then delete the actual files (these calls will handle their own SaveAsync)
			foreach (var existingFile in existingFiles)
			{
				await filesService.DeleteFile(existingFile.FileID, ct);
				if (existingFile.ThumbnailFileID != null)
					await filesService.DeleteFile(existingFile.ThumbnailFileID.Value, ct);
			}
		}

		var fileUpload = new FileUploadRequestDto
		{
			ContentType = request.ContentType,
			FileBytes = request.FileBytes,
			FileName = request.FileName,
		};

		ct.ThrowIfCancellationRequested();

		Result<ImageUploadResultDto> uploadResponse;

		if (fileUpload.ContentType == "application/pdf")
		{
			var pdfUploadResult = await filesService.UploadFileAsync(fileUpload, ct);

			if (!pdfUploadResult.IsSuccess)
				return Result<FileUploadResultDto>.Failure(pdfUploadResult.Error, pdfUploadResult.ErrorType);

			// Map FileUploadResult to ImageUploadResult  
			uploadResponse = Result<ImageUploadResultDto>.Success(new ImageUploadResultDto
			{
				FileId = pdfUploadResult.Value.FileId,
				FileName = pdfUploadResult.Value.FileName,
				FilePath = $"api/ProUser/{proUserId}/Files/{pdfUploadResult.Value.FileId}",
				ThumbnailFileId = null, // Assuming no thumbnail for PDF  
				ThumbnailFileName = string.Empty,
				ThumbnailFilePath = string.Empty
			});
		}
		else
		{
			uploadResponse = await filesService.UploadImageAsync(fileUpload, ct);
		}

		if (!uploadResponse.IsSuccess)
			return Result<FileUploadResultDto>.Failure(uploadResponse?.Error, uploadResponse?.ErrorType);

		var newProUserFile = new ProUserFile
		{
			FileID = uploadResponse.Value.FileId,
			UserID = proUserId,
			ThumbnailFileID = uploadResponse.Value.ThumbnailFileId,
			ProFileType = request.ProFileType,
			DateCreated = DateTimeOffset.UtcNow
		};

		repository.AddNew(newProUserFile);
		ct.ThrowIfCancellationRequested();
		await repository.SaveAsync(ct);

		ct.ThrowIfCancellationRequested();
		var proUserFileItem = await repository
			.Query<ProUserFile>()
			.ByFileID(newProUserFile.FileID)
			.Include(x => x.File)
			.Include(x => x.ThumbnailFile)
			.FirstOrDefaultAsync(ct);

		return Result<FileUploadResultDto>.Success(ProUserMapper.MapToFileResult(proUserFileItem));
	}

	public async Task<Result> DeleteFile(int proUserId, int fileId, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		var proUserFile = await repository
			.Query<ProUserFile>()
			.Include(x => x.File)
			.Include(x => x.ThumbnailFile)
			.ByUserID(proUserId)
			.ByFileID(fileId)
			.FirstOrDefaultAsync(ct);

		if (proUserFile == null)
			return Result<FileDownloadResultDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		Result deleteResults;

		// Use soft delete for Insurance files, hard delete for others
		if (proUserFile.ProFileType == ProUserFileType.Insurance)
		{
			// Soft delete the main file
			deleteResults = await filesService.SoftDeleteFile(fileId, ct);

			// Soft delete the thumbnail file if it exists
			if (proUserFile.ThumbnailFileID != null)
				await filesService.SoftDeleteFile(proUserFile.ThumbnailFileID.Value, ct);
		}
		else
		{
			// Hard delete the main file
			deleteResults = await filesService.DeleteFile(fileId, ct);

			// Hard delete the thumbnail file if it exists
			if (proUserFile.ThumbnailFileID != null)
				await filesService.DeleteFile(proUserFile.ThumbnailFileID.Value, ct);

			// No need to delete the pro user file, since the above delete already cascade deletes that also
		}

		if (deleteResults.IsSuccess)
			return Result.Success();
		else
			return Result.Failure(deleteResults.Error, deleteResults.ErrorType);
	}
}
