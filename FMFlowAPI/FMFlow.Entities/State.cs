using System.ComponentModel.DataAnnotations;

namespace FMFlow.Entities;

public class State
{
	[Key]
	public string Abbreviation { get; set; } = string.Empty;
	public string StateName { get; set; } = string.Empty;
}
