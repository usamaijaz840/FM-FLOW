using FluentValidation;
using FMFlow.Identity.Interface.DTOs;
using FMFlow.Common.Extensions;

namespace FMFlow.Leads.Service.Validators;

public class SavePasswordRequestDTOValidator : AbstractValidator<SavePasswordRequestDto>
{
	public SavePasswordRequestDTOValidator()
	{
		RuleFor(x => x.Password)
			.ApplyPasswordPolicy();

	}
}
