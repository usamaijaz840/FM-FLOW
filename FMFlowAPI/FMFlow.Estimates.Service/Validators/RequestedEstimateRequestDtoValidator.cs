using FluentValidation;
using FMFlow.Estimates.Interface.DTOs;

namespace FMFlow.Estimates.Service.Validators;

public class RequestedEstimateRequestDtoValidator : AbstractValidator<RequestedEstimateRequestDto>
{
	public RequestedEstimateRequestDtoValidator()
	{
		RuleFor(x => x.Name)
			.NotEmpty()
			.MaximumLength(200);

		RuleFor(x => x.EstimateTypeId)
			.GreaterThan(0);
	}
}
