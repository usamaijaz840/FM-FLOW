using FMFlow.Entities;
using FMFlow.Estimates.Interface.DTOs;
using Riok.Mapperly.Abstractions;

namespace FMFlow.Estimates.Service.Mappers;

[Mapper]
public partial class JobNoteMapper
{
	public partial JobNoteResponseDto MapToJobNoteResponseDto(JobNote jobNote);

	public partial void UpdateJobNote(JobNoteRequestDto request, JobNote jobNote);
}
