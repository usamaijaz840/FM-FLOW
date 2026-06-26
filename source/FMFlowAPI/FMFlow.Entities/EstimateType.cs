using System.ComponentModel.DataAnnotations;

namespace FMFlow.Entities;

public class EstimateType
{
	[Key]
	public int EstimateTypeId { get; set; }

	public string EstimateTypeName { get; set; } = string.Empty;
	
	public string EstimateTypeAbbreviation { get; set; } = string.Empty;
}
