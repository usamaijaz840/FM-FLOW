namespace FMFlow.ProUser.Interface.DTOs;

public record TimeZoneDTO(
	int TimeZoneId,
	string Name,
	string SystemTimeZoneId
);
