using FluentValidation;
using FMFlow.Estimates.Interface.DTOs;

namespace FMFlow.Estimates.Service.Validators;

public class ScheduledEstimateRequestDtoValidator : AbstractValidator<ScheduledEstimateRequestDto>
{
	public ScheduledEstimateRequestDtoValidator()
	{
		RuleFor(x => x.ProUserID)
			.NotEmpty()
			.GreaterThan(0);

		RuleFor(x => x.ProjectID)
			.NotEmpty()
			.GreaterThan(0);

		RuleFor(x => x.ScheduledDateTime)
			.NotEmpty()
			.Must(x => x > DateTimeOffset.UtcNow);
	}
}
