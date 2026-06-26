using System.Text.Json;
using FluentValidation;
using FMFlow.Estimates.Interface.DTOs;

namespace FMFlow.Estimates.Service.Validators;

public class EstimateRequestDtoValidator : AbstractValidator<EstimateRequestDto>
{
	public EstimateRequestDtoValidator()
	{
		RuleFor(x => x.RequestedEstimateID)
			.NotEmpty()
			.GreaterThan(0);

		RuleFor(x => x.ProUserID)
			.NotEmpty()
			.GreaterThan(0);

		RuleFor(x => x.Attributes)
			.Must(BeJsonObject)
			.When(x => x.Attributes.HasValue)
			.WithMessage("Attributes must be a JSON object.");

	}

	private bool BeJsonObject(JsonElement? element)
	{
		return element?.ValueKind == JsonValueKind.Object;
	}
}
