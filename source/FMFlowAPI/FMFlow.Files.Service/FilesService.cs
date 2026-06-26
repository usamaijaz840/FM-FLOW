using Amazon.Runtime.Internal;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using EFRepository;
using FileTypeChecker;
using FMFlow.Entities;
using FMFlow.Files.Interface;
using FMFlow.Files.Interface.DTOs;
using FMFlow.FlowAPI.Interface;
using ImageMagick;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;

namespace FMFlow.Files.Service;

public class FilesService(
	IOptions<FileUploadSettings> fileUploadOptions,
	IRepository repository,
	IAmazonS3 s3Client,
	ITransferUtility transferUtility) : IFilesService
{
	private readonly FileUploadSettings fileUploadSettings = fileUploadOptions.Value;
	private static readonly string[] allValidExtensions = [".png", ".jpg", ".jpeg", ".heic", ".heif", ".pdf"];
	private static readonly string[] validExtensionsWithFileValidation = ["png", "jpg", "jpeg", "heic", "heif"];
	private static readonly string[] validExtensionsWithoutFileValidation = [".heic", ".heif"];

	public async Task<Result> DeleteFile(int fileID, CancellationToken ct)
	{
		var bucketName = fileUploadSettings.S3BucketName;

		ct.ThrowIfCancellationRequested();
		var fileItem = await repository.Query<FileItem>()
			.ByFileID(fileID)
			.FirstOrDefaultAsync(ct);

		if (fileItem == null)
			return Result.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		var deleteRequest = new DeleteObjectRequest
		{
			BucketName = bucketName,
			Key = fileItem.Name
		};

		try
		{
			ct.ThrowIfCancellationRequested();
			await s3Client.DeleteObjectAsync(deleteRequest, ct);
		}
		catch (Exception ex)
		{
			return Result.Failure($"Failed to delete file from S3: {ex.Message}", ResultErrorType.BadRequest);
		}

		repository.Delete(fileItem);
		ct.ThrowIfCancellationRequested();
		await repository.SaveAsync(ct);

		return Result.Success();
	}

	public async Task<Result> SoftDeleteFile(int fileID, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		var fileItem = await repository.Query<FileItem>()
			.ByFileID(fileID)
			.FirstOrDefaultAsync(ct);

		if (fileItem == null)
			return Result.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

		// Perform soft delete instead of hard delete
		fileItem.IsDeleted = true;
		fileItem.DateDeleted = DateTimeOffset.UtcNow;

		ct.ThrowIfCancellationRequested();
		await repository.SaveAsync(ct);

		return Result.Success();
	}

	public async Task<Result<FileDownloadResultDto>> GetFile(string fileKey, CancellationToken ct)
	{
		var bucketName = fileUploadSettings.S3BucketName;

		var request = new GetObjectRequest
		{
			BucketName = bucketName,
			Key = fileKey
		};

		try
		{
			using var response = await s3Client.GetObjectAsync(request, ct);
			using var responseStream = response.ResponseStream;
			using var memoryStream = new MemoryStream();

			ct.ThrowIfCancellationRequested();
			await responseStream.CopyToAsync(memoryStream, ct);
			memoryStream.Position = 0;

			return Result<FileDownloadResultDto>.Success(new FileDownloadResultDto
			{
				FileBytes = memoryStream.ToArray(),
				ContentType = response.Headers["Content-Type"]
			});
		}
		catch (HttpErrorResponseException ex)
		{
			return Result<FileDownloadResultDto>.Failure(ex.Message, ResultErrorType.BadRequest);
		}
	}

	public async Task<Result<ImageUploadResultDto>> UploadImageAsync(FileUploadRequestDto fileRequest, CancellationToken ct)
	{
		var extension = Path.GetExtension(fileRequest.FileName).ToLowerInvariant();

		if (!IsExtensionValid(extension, fileRequest.FileBytes))
			return Result<ImageUploadResultDto>.Failure(
				$"Invalid file type. Valid types are: {string.Join(", ", allValidExtensions.Select(e => e.TrimStart('.').ToUpperInvariant()))}.",
				ResultErrorType.BadRequest
			);

		if (extension == ".heic" || extension == "heif")
		{
			fileRequest.FileBytes = ConvertHeicToJpg(fileRequest.FileBytes);
			extension = ".jpg";
			fileRequest.FileName = $"{Path.GetFileNameWithoutExtension(fileRequest.FileName)}{extension}";
			fileRequest.ContentType = "image/jpeg";
		}

		ct.ThrowIfCancellationRequested();
		var uploadResult = await UploadFileAsync(fileRequest, ct);

		if (!uploadResult.IsSuccess)
			return Result<ImageUploadResultDto>.Failure(uploadResult.Error);

		var thumbnail = CreateThumbnail(fileRequest.FileBytes);
		var thumbnailKey = GenerateUniqueFileKey(extension);

		var thumbnailRequest = new FileUploadRequestDto
		{
			FileBytes = thumbnail,
			FileName = AddThumbnailSuffix(fileRequest.FileName),
			ContentType = fileRequest.ContentType
		};

		ct.ThrowIfCancellationRequested();
		var uploadThumbnailResult = await UploadFileAsync(thumbnailKey, thumbnailRequest, ct);

		if (!uploadThumbnailResult.IsSuccess)
			return Result<ImageUploadResultDto>.Failure(uploadThumbnailResult.Error);


		return Result<ImageUploadResultDto>.Success(new ImageUploadResultDto
		{
			FileId = uploadResult.Value.FileId,
			FileName = uploadResult.Value.FileName,
			ThumbnailFileId = uploadThumbnailResult.Value.FileId,
			ThumbnailFileName = uploadThumbnailResult.Value.FileName,
		});
	}

	public async Task<Result<FileUploadResultDto>> UploadFileAsync(FileUploadRequestDto fileRequest, CancellationToken ct = default)
	{
		var extension = Path.GetExtension(fileRequest.FileName).ToLowerInvariant();
		string key = GenerateUniqueFileKey(extension);

		ct.ThrowIfCancellationRequested();
		return await UploadFileAsync(key, fileRequest, ct);
	}

	private static string GenerateUniqueFileKey(string extension)
	{
		return $"{Guid.NewGuid()}{extension}";
	}

	private async Task<Result<FileUploadResultDto>> UploadFileAsync(string keyName, FileUploadRequestDto fileRequest, CancellationToken ct)
	{
		if (fileRequest == null || fileRequest.FileBytes == null || fileRequest.FileBytes.Length == 0)
			return Result<FileUploadResultDto>.Failure("No file uploaded.", ResultErrorType.BadRequest);

		var maxFileSize = fileUploadSettings.MaxFileSize;
		if (fileRequest.FileBytes.Length > maxFileSize)
			return Result<FileUploadResultDto>.Failure($"File size exceeds the maximum allowed size of {maxFileSize} bytes.", ResultErrorType.BadRequest);

		var bucketName = fileUploadSettings.S3BucketName;

		using var newMemoryStream = new MemoryStream(fileRequest.FileBytes);
		var uploadRequest = new TransferUtilityUploadRequest
		{
			InputStream = newMemoryStream,
			Key = keyName,
			BucketName = bucketName,
			ContentType = fileRequest.ContentType
		};

		ct.ThrowIfCancellationRequested();
		await transferUtility.UploadAsync(uploadRequest, ct);

		var newFileItem = new FileItem
		{
			Name = fileRequest.FileName,
			Key = keyName,
			DateCreated = DateTimeOffset.UtcNow,
			IsDeleted = false,
			ContentType = fileRequest.ContentType
		};

		repository.AddNew(newFileItem);

		ct.ThrowIfCancellationRequested();
		await repository.SaveAsync(ct);

		return Result<FileUploadResultDto>.Success(new FileUploadResultDto
		{
			FileId = newFileItem.FileID,
			Message = "File uploaded successfully",
			FileName = newFileItem.Name
		});
	}

	protected static byte[] ConvertHeicToJpg(byte[] heicBytes)
	{
		using var image = new MagickImage(heicBytes);
		image.Format = MagickFormat.Jpg;
		return image.ToByteArray();
	}

	protected static bool IsExtensionValid(string fileNameExtension, byte[] fileBytes)
	{
		if (string.IsNullOrEmpty(fileNameExtension) || !(allValidExtensions).Contains(fileNameExtension))
			return false;

		if (validExtensionsWithoutFileValidation.Contains(fileNameExtension))
			return true;

		var isRecognizableType = FileTypeValidator.IsTypeRecognizable(fileBytes);

		if (!isRecognizableType)
			return false;

		var fileType = FileTypeValidator.GetFileType(fileBytes);

		if (fileType == null)
			return false;

		return validExtensionsWithFileValidation.Any(e => e.Equals(fileType.Extension, StringComparison.CurrentCultureIgnoreCase));
	}

	protected static byte[] CreateThumbnail(byte[] imageBytes, int targetShortSize = 150)
	{
		IImageFormat format = Image.DetectFormat(imageBytes) ?? throw new InvalidOperationException("Unsupported image format.");

		using var image = Image.Load(imageBytes);
		int originalWidth = image.Width;
		int originalHeight = image.Height;

		// Calculate new size preserving aspect ratio where the smaller side is 150
		float scale = (float)targetShortSize / Math.Min(originalWidth, originalHeight);
		int newWidth = (int)(originalWidth * scale);
		int newHeight = (int)(originalHeight * scale);

		image.Mutate(x => x.Resize(newWidth, newHeight));

		using var outputStream = new MemoryStream();
		image.Save(outputStream, format);
		return outputStream.ToArray();
	}

	private static string AddThumbnailSuffix(string fileName)
	{
		var extension = Path.GetExtension(fileName);
		var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
		return $"{nameWithoutExtension}-thumbnail{extension}";
	}
}
