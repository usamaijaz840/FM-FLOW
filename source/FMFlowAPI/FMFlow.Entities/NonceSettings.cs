using FMFlow.Entities;

namespace FMFlow.Entities;

public class NonceSettings
{
	public const string SectionName = "NonceSettings";

	public required Dictionary<NonceType, NonceConfiguration> NonceConfigurations { get; set; }

}
