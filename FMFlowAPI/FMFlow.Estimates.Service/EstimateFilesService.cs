using EFRepository;
using FMFlow.AccessValidation;
using FMFlow.Entities;
using FMFlow.Estimates.Interface;
using FMFlow.Estimates.Interface.DTOs;
using FMFlow.Estimates.Service.Mappers;
using FMFlow.Files.Interface;
using FMFlow.Files.Interface.DTOs;
using FMFlow.FlowAPI.Interface;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.Estimates.Service;

public class EstimateFilesService(
	IRepository repository,
	IFilesService filesService,
	IAccessValidator accessValidator,
	IJobCompletionService jobCompletionService) : IEstimateFilesService
{
	public async Task<Result<List<FileResponseDto>>> GetFiles(int estimateId, bool includeSignatures, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		var estimate = await repository
			.Query<Estimate>()
			.Include(e => e.RequestedEstimate)
			.ThenInclude(re => re.Project)
			.ThenInclude(p => p.Lead)
			.ThenInclude(l => l.Address)
			.ByEstimateID(estimateId)
			.FirstOrDefaultAsync(ct);

		if (estimate == null)
			return Result<List<FileResponseDto>>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToEstimate(estimate, ct);

		if (!accessResult.IsSuccess)
			return Result<List<FileResponseDto>>.Failure(accessResult.Error!, accessResult.ErrorType);

		ct.ThrowIfCancellationRequested();

		var estimateFiles = await repository
			.Query<FileItemToEstimate>()
			.ByEstimateID(estimateId)
			.Where(x =>
				includeSignatures ||
				(x.FileType != FileItemToEstimate.EstimateFileType.ProSignature &&
				 x.FileType != FileItemToEstimate.EstimateFileType.CustomerSignature))
			.Include(x => x.File)
			.Include(x => x.ThumbnailFile)
			.ToListAsync(ct);

		var responseDtos = estimateFiles.Select(eF => EstimateMapper.MapToFile(eF, estimate.EstimateID)).ToList();

		return Result<List<FileResponseDto>>.Success(responseDtos);
	}

	public async Task<Result<FileDownloadResultDto>> DownloadFile(int estimateId, int fileId, bool shouldGetThumbnail, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		var estimate = await repository
			.Query<Estimate>()
			.Include(e => e.RequestedEstimate)
			.ThenInclude(re => re.Project)
			.ThenInclude(p => p.Lead)
			.ThenInclude(l => l.Address)
			.ByEstimateID(estimateId)
			.FirstOrDefaultAsync(ct);

		if (estimate == null)
			return Result<FileDownloadResultDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToEstimate(estimate, ct);

		if (!accessResult.IsSuccess)
			return Result<FileDownloadResultDto>.Failure(accessResult.Error!, accessResult.ErrorType);

		var estimateFileQuery = repository
			.Query<FileItemToEstimate>()
			.ByEstimateID(estimateId)
			.ByFileID(fileId);

		ct.ThrowIfCancellationRequested();

		FileItem? estimateFile;

		if (shouldGetThumbnail)
		{
			estimateFile = await estimateFileQuery
				.Select(x => x.ThumbnailFile)
				.FirstOrDefaultAsync(ct);
		}
		else
		{
			estimateFile = await estimateFileQuery
				.Select(x => x.File)
				.FirstOrDefaultAsync(ct);
		}

		if (estimateFile == null)
			return Result<FileDownloadResultDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var fileResult = await filesService.GetFile(estimateFile.Key, ct);
		fileResult.Value!.FileName = estimateFile.Name;

		return fileResult;
	}

	public async Task<Result<FileUploadResultDto>> CreateEstimateFile(int estimateId, EstimateFileUploadRequestDto request, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		var estimate = await repository
			.Query<Estimate>()
			.Include(e => e.Job)
			.Include(e => e.RequestedEstimate)
			.ThenInclude(re => re.Project)
			.ThenInclude(p => p.Lead)
			.ThenInclude(l => l.Address)
			.ByEstimateID(estimateId)
			.FirstOrDefaultAsync(ct);

		if (estimate == null)
			return Result<FileUploadResultDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToEstimate(estimate, ct);

		if (!accessResult.IsSuccess)
			return Result<FileUploadResultDto>.Failure(accessResult.Error!, accessResult.ErrorType);

		var fileNameAlreadyUsed = await repository
			.Query<FileItemToEstimate>()
			.ByEstimateID(estimateId)
			.Where(fileItem => fileItem.File.Name == request.FileName)
			.AnyAsync(ct);

		if (fileNameAlreadyUsed)
			return Result<FileUploadResultDto>.Failure("File name already in use for this estimate.");

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
				FilePath = $"api/Estimates/{estimate.EstimateID}/Files/{pdfUploadResult.Value.FileId}",
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

		var newFileItemToEstimate = new FileItemToEstimate
		{
			FileID = uploadResponse.Value.FileId,
			EstimateID = estimateId,
			ThumbnailFileID = uploadResponse.Value.ThumbnailFileId,
			DateCreated = DateTimeOffset.UtcNow,
			FileType = request.EstimateFileType ?? FileItemToEstimate.EstimateFileType.General
		};

		repository.AddNew(newFileItemToEstimate);
		ct.ThrowIfCancellationRequested();

		await repository.SaveAsync(ct);

		ct.ThrowIfCancellationRequested();

		if (estimate.Job != null)
			await jobCompletionService.CloseJobIfEligible(estimate.Job.JobId, ct);

		var estimateFileItem = await repository
			.Query<FileItemToEstimate>()
			.ByFileID(newFileItemToEstimate.FileID)
			.Include(x => x.File)
			.Include(x => x.ThumbnailFile)
			.FirstAsync(ct);

		return Result<FileUploadResultDto>.Success(EstimateMapper.MapEstimateFileToImageUploadResult(estimateFileItem));
	}

	public async Task<Result> DeleteEstimateFile(int estimateId, int fileId, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		var estimate = await repository
			.Query<Estimate>()
			.Include(e => e.RequestedEstimate)
			.ThenInclude(re => re.Project)
			.ThenInclude(p => p.Lead)
			.ThenInclude(l => l.Address)
			.ByEstimateID(estimateId)
			.FirstOrDefaultAsync(ct);

		if (estimate == null)
			return Result.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var accessResult = await accessValidator.ValidateAccessToEstimate(estimate, ct);

		if (!accessResult.IsSuccess)
			return Result.Failure(accessResult.Error!, accessResult.ErrorType);

		ct.ThrowIfCancellationRequested();
		var estimateFile = await repository
			.Query<FileItemToEstimate>()
			.Include(x => x.File)
			.Include(x => x.ThumbnailFile)
			.ByEstimateID(estimateId)
			.ByFileID(fileId)
			.FirstOrDefaultAsync(ct);

		if (estimateFile == null)
			return Result<FileDownloadResultDto>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		// Signatures are legally sensitive and must never be deletable once saved.
		if (estimateFile.FileType is FileItemToEstimate.EstimateFileType.ProSignature
			or FileItemToEstimate.EstimateFileType.CustomerSignature)
		{
			return Result.Failure("Signature files cannot be deleted.", ResultErrorType.PermissionDenied);
		}

		var deleteResults = await filesService.DeleteFile(fileId, ct);
		if (estimateFile.ThumbnailFileID != null)
			await filesService.DeleteFile(estimateFile.ThumbnailFileID.Value, ct);

		await repository.SaveAsync(ct);

		if (deleteResults.IsSuccess)
			return Result.Success();
		else
			return Result.Failure(deleteResults.Error, deleteResults.ErrorType);
	}
}
