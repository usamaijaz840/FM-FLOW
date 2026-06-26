namespace FMFlow.Pro.Interface.Dtos;

public record ProUserWithTokenDto(
  ProUserDto ProUser,
  string AccessToken
);
