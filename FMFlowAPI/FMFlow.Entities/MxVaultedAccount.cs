using System.ComponentModel.DataAnnotations;

namespace FMFlow.Entities;

public class MxVaultedAccount // this might be moved to Redis in the future
{
	[Key]
	public int AccountId { get; set; }

	public long MxInternalAccountId { get; set; }

	public long MxCustomerId { get; set; }

	public string Token { get; set; } = null!;

	public DateTimeOffset ExpirationTime { get; set; } = DateTimeOffset.UtcNow.AddMinutes(30);
}
