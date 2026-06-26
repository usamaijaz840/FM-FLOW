using System.Text.Json;
using FMFlow.Entities;
using FMFlow.Estimates.Interface.DTOs;
using FMFlow.Files.Interface.DTOs;
using Riok.Mapperly.Abstractions;

namespace FMFlow.Estimates.Service.Mappers;

[Mapper]
public partial class EstimateMapper
{
	public partial RequestedEstimate MapToRequestedEstimate(RequestedEstimateRequestDto request);

	public partial RequestedEstimateResponseDto MapToRequestedEstimateResponse(RequestedEstimate requestedEstimate);

	public partial Estimate MapToEstimate(EstimateRequestDto request);

	public partial EstimateResponseDto MapToEstimateResponse(Estimate source);

	[MapProperty("RequestedEstimate.ProjectID", "ProjectID")]
	public partial SearchEstimatesResponseDto MapToSearchEstimatesResponse(Estimate source);

	public static FileResponseDto MapToFile(FileItemToEstimate estimateFile, int estimateId)
	{
		return new FileResponseDto
		{
			FileID = estimateFile.FileID,
			FileName = estimateFile.File.Name,
			FilePath = $"api/Estimate/{estimateId}/Files/{estimateFile.FileID}",
			ContentType = estimateFile.File.ContentType,
			EstimateFileType = estimateFile.FileType,
			ThumbnailFileID = estimateFile.ThumbnailFileID,
			ThumbnailFileName = estimateFile.ThumbnailFile?.Name,
			ThumbnailFilePath = $"api/Estimate/{estimateId}/Thumbnails/{estimateFile.ThumbnailFile?.FileID}",
		};
	}

	public static KanbanEstimateOrJobResponseDto MapToKanbanDto(Estimate source) => new(
			source.EstimateID,
			source.Status.ToString(),
			source.RequestedEstimate.Name,
			source.RequestedEstimate.Project.Address.ToString(),
			source.ProUser.GetFullName(),
			source.Total,
			source.DepositHasBeenPaid,
			source.RequestedEstimate.EstimateType.EstimateTypeName,
			source.RequestedEstimate.Project.Lead != null ? $"{source.RequestedEstimate.Project.Lead.FirstName} {source.RequestedEstimate.Project.Lead.LastName}" : null);

	public static ImageUploadResultDto MapEstimateFileToImageUploadResult(FileItemToEstimate source)
	{
		return new ImageUploadResultDto
		{
			FileId = source.File.FileID,
			FileName = source.File.Name,
			FilePath = $"api/Estimate/{source.EstimateID}/Files/{source.File.FileID}",
			ContentType = source.File.ContentType,
			Message = "File uploaded successfully.",
			ThumbnailFileId = source.ThumbnailFileID,
			ThumbnailFileName = source.ThumbnailFile?.Name,
			ThumbnailFilePath = source.ThumbnailFileID.HasValue
				? $"api/Estimate/{source.EstimateID}/Thumbnails/{source.ThumbnailFileID.Value}"
				: null,
		};
	}

	// Methods used internaly by mapperly to map JsonDocument to JsonElement and back:
	private static JsonDocument? MapNullableJsonElementToJsonDocument(JsonElement? element)
	=> element is null ? null : JsonDocument.Parse(element.Value.GetRawText());

	private static JsonElement? MapJsonDocumentToNullableJsonElement(JsonDocument? doc)
	=> doc?.RootElement;
}
