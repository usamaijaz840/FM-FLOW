namespace FMFlow.Customers.Interface.DTOs;

public record CustomerRequestDto
{
	public string? nonce { get; set; }
	public string Password { get; set; }
}
