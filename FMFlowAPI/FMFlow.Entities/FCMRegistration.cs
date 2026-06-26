using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FMFlow.Entities;

public class FCMRegistration
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int FCMRegistrationId { get; set; }

	[ForeignKey(nameof(User))]
	public int UserId { get; set; }
	public virtual FlowUser User { get; set; } = null!;

	public string Token { get; set; } = null!;

	public DateTime CheckInDateTime { get; set; } = DateTime.UtcNow;
}
