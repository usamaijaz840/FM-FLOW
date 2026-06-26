using FMFlow.Entities;
using FMFlow.Estimates.Interface.DTOs;
using Riok.Mapperly.Abstractions;

namespace FMFlow.Estimates.Service.Mappers;

[Mapper]
public partial class EstimateNoteMapper
{
    public partial EstimateNoteResponseDto MapToEstimateNoteResponseDto(EstimateNote estimateNote);
    
    public partial void UpdateEstimateNote(EstimateNoteRequestDto request, EstimateNote estimateNote);
} 