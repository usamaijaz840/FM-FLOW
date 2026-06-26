using System.ComponentModel.DataAnnotations.Schema;
using FMFlow.Entities.ModelInterfaces;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.Entities;

[PrimaryKey(nameof(Zipcode), nameof(UserID))]
public class ProUserToProZipcode : IHasDeleted
{
	[DatabaseGenerated(DatabaseGeneratedOption.None)]
	[ForeignKey(nameof(ProZipcode))]
	public string Zipcode { get; set; }

	[DatabaseGenerated(DatabaseGeneratedOption.None)]
	[ForeignKey(nameof(ProUserDetail))]
	public int UserID { get; set; }

	public virtual ZipCode ProZipcode { get; set; }

	public virtual ProUserDetail? ProUserDetail { get; set; }

	public DateTimeOffset DateCreated { get; set; } = DateTime.UtcNow;

	public DateTimeOffset? DateDeleted { get; set; }

	public bool IsDeleted { get; set; }
}
