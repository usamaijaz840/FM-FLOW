using FluentValidation;
using FMFlow.Common.Extensions;
using FMFlow.Employees.Interface.DTOs;
using FMFlow.Identity.Interface;

namespace FMFlow.Employees.Service.Validators;

public class EmployeeDtoValidator : AbstractValidator<EmployeeRequestDto>
{
	public EmployeeDtoValidator()
	{
		RuleFor(x => x.FirstName)
			.NotEmpty()
			.MaximumLength(50);

		RuleFor(x => x.LastName)
			.NotEmpty()
			.MaximumLength(50);

		RuleFor(x => x.Email)
			.NotEmpty()
			.MaximumLength(100)
			.EmailAddress();

		RuleFor(x => x.PhoneNumber)
			.NotEmpty()
			.MaximumLength(50);

		RuleFor(x => x.Role)
			.NotEmpty()
			.Must(role => !int.TryParse(role, out _))
			.WithMessage("Role must be a string value (e.g., 'AccountManager'), not a numeric value.")
			.Must(role => Enum.GetNames<Roles>().Contains(role))
			.WithMessage($"Invalid role specified. Valid roles are: {string.Join(", ", Enum.GetNames<Roles>())}");

		RuleFor(x => x.Biography)
			.MaximumLength(3000);

		RuleFor(x => x.Memo)
			.MaximumLength(3000);

		RuleFor(x => x.AddressLine2)
			.MaximumLength(100);

		// If any address field is provided, all address fields must be provided
		When(x => !string.IsNullOrWhiteSpace(x.AddressLine1) ||
			!string.IsNullOrWhiteSpace(x.City) ||
			!string.IsNullOrWhiteSpace(x.State) ||
			!string.IsNullOrWhiteSpace(x.ZipCode), () =>
		{
			RuleFor(x => x.AddressLine1)
				.NotEmpty()
				.MaximumLength(100)
				.WithMessage("Address Line 1 is required when any other address field (City, State, or Zip Code) is provided.");

			RuleFor(x => x.City)
				.NotEmpty()
				.MaximumLength(50)
				.WithMessage("City is required when any other address field (Address Line 1, State, or Zip Code) is provided.");

			RuleFor(x => x.State)
				.NotEmpty()
				.MinimumLength(2)
				.MaximumLength(2)
				.WithMessage("State is required when any other address field (Address Line 1, City, or Zip Code) is provided.");

			RuleFor(x => x.ZipCode)
				.ApplyZipCodePolicy();
		});

		RuleFor(x => x.TwilioNumber)
			.MaximumLength(50);

		RuleFor(x => x.TwilioCallerID)
			.MaximumLength(50);
	}
}
