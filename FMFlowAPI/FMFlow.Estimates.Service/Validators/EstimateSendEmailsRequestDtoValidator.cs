using FluentValidation;
using FluentValidation.Validators;
using FMFlow.Estimates.Interface.DTOs;

namespace FMFlow.Estimates.Service.Validators;

public class EstimateSendEmailsRequestDtoValidator : AbstractValidator<EstimateSendEmailsRequestDto>
{
	public EstimateSendEmailsRequestDtoValidator()
	{
		RuleForEach(x => x.AdditionalEmailAddresses)
			.Cascade(CascadeMode.Stop)
			.NotEmpty().WithMessage("Email at index {CollectionIndex} is empty")
			.Must(s => !string.IsNullOrWhiteSpace(s)).WithMessage("Email at index {CollectionIndex} is whitespace")
			.Must(s => s == s.Trim()).WithMessage("Email at index {CollectionIndex} has leading/trailing spaces")
			.EmailAddress(EmailValidationMode.AspNetCoreCompatible)
			.WithMessage("'{PropertyValue}' at index {CollectionIndex} is not a valid email address");

		RuleFor(x => x.AdditionalEmailAddresses)
			.Must(list => list.Select(e => e.Trim().ToLowerInvariant()).Distinct().Count() == list.Count)
			.WithMessage("Duplicate email addresses are not allowed");
	}
}
