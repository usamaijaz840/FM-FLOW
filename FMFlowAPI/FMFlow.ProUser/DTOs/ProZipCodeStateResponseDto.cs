namespace FMFlow.ProUser.Interface.DTOs;

public record ProZipCodeStateResponseDto
{
	public List<StateWithCounties> States { get; init; } = null!;
}
