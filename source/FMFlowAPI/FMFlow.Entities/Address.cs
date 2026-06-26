using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FMFlow.Entities;

public class Address
{
	[Key]
	public int AddressID { get; set; }

	public string Line1 { get; set; } = string.Empty;

	public string? Line2 { get; set; }

	[ForeignKey(nameof(State))]
	public string? StateId { get; set; }

	public virtual State? State { get; set; }

	public string City { get; set; } = string.Empty;

	public string ZipCode { get; set; } = string.Empty;

	public override string ToString()
	{
		return string.Join(", ",
			new[] { Line1, Line2, City, State?.StateName, ZipCode }
			.Where(s => !string.IsNullOrWhiteSpace(s)));
	}
}
