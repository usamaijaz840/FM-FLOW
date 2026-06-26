using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.Entities;

[Index(nameof(SessionId), nameof(ProId), IsUnique = true)]
public class ChatBotSessionPro
{
	[Key]
	public int ChatBotSessionProId { get; set; }

	public DateTimeOffset ExpireDateTime { get; set; } = DateTimeOffset.UtcNow.AddHours(4);

	[Required]
	public string SessionId { get; set; } = null!;

	[Required]
	[ForeignKey(nameof(Pro))]
	public int ProId { get; set; }
	public virtual FlowUser Pro { get; set; } = null!;
}
