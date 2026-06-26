using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace FMFlow.Entities;

public class Nonce
{
	[Key]
	public int Id { get; set; }

	[Required]
	public string Value { get; set; } = string.Empty;

	[Required]
	public int EntityId { get; set; }

	[Required]
	public NonceType Type { get; set; } = NonceType.Unassigned;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public DateTime ExpiresAt { get; set; }

	public bool Consumed { get; set; } = false;

	public DateTime? ConsumedAt { get; set; }

	public void SetExpiration(string duration)
	{
		ExpiresAt = CreatedAt.Add(TimeSpan.Parse(duration));
	}

	public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}
