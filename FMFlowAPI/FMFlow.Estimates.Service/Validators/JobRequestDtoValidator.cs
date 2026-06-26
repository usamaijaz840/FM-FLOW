using FluentValidation;
using FMFlow.Estimates.Interface.DTOs;

namespace FMFlow.Estimates.Service.Validators;

public class JobRequestDtoValidator : AbstractValidator<JobRequestDto>
{
	public JobRequestDtoValidator()
	{
		RuleFor(x => x.EstimateId)
			.NotEmpty()
			.GreaterThan(0);
	}
}
