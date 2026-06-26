using FluentValidation;
using FMFlow.Estimates.Interface.DTOs;

namespace FMFlow.Estimates.Service.Validators;

public class ResendEstimateReviewEmailRequestDtoValidator : AbstractValidator<ResendEstimateReviewEmailRequestDto>
{
	public ResendEstimateReviewEmailRequestDtoValidator()
	{
		RuleFor(x => x.Nonce)
			.NotEmpty();

		RuleFor(x => x.EstimateId)
			.GreaterThan(0);
	}
}
