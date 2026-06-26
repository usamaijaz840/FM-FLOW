using FMFlow.Estimates.Interface.DTOs;
using FluentValidation;

namespace FMFlow.Estimates.Service.Validators;

public class EstimateNoteRequestDtoValidator : AbstractValidator<EstimateNoteRequestDto>
{
    public EstimateNoteRequestDtoValidator()
    {
        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("Note cannot be empty")
            .MaximumLength(3000).WithMessage("Note cannot exceed 3000 characters");
    }
} 