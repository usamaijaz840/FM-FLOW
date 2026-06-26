using FluentValidation;
using FMFlow.Estimates.Interface.DTOs;

namespace FMFlow.Estimates.Service.Validators;

public class JobNoteRequestDtoValidator : AbstractValidator<JobNoteRequestDto>
{
	public JobNoteRequestDtoValidator()
	{
		RuleFor(x => x.Note)
			.NotEmpty().WithMessage("Note cannot be empty")
			.MaximumLength(3000).WithMessage("Note cannot exceed 3000 characters");
	}
}
