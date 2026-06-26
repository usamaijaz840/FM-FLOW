using System.Diagnostics.Metrics;
using FMFlow.Entities;
using FMFlow.Pro.Interface.Dtos;
using FMFlow.ProUser.Interface.DTOs;
using Riok.Mapperly.Abstractions;

namespace FMFlow.ProUser.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, AllowNullPropertyAssignment = false)]
public partial class ProCountiesMapper
{
	public CountyDto MapToCountyDto(string county)
	{
		return new CountyDto { Name = county };
	}
}
