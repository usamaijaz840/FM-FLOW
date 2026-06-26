using FluentValidation;
using FMFlow.Identity.Interface.DTOs;

namespace FMFlow.Identity.Service.Validators;

public class ServiceAccountClientTokenRequestDtoValidator : AbstractValidator<ServiceAccountClientTokenRequestDto>
{
	public ServiceAccountClientTokenRequestDtoValidator()
	{
		RuleFor(x => x.ClientId)
			.NotEmpty();

		RuleFor(x => x.ClientSecret)
			.NotEmpty();
	}
}
