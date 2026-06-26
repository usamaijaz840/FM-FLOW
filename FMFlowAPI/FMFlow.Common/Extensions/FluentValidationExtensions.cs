using FluentValidation;

namespace FMFlow.Common.Extensions;

public static class FluentValidationExtensions
{
	/// <summary>
	/// Applies the project's canonical password rules to a rule builder for a string password.
	/// Keeps rules centralized so multiple validators can reuse the same policy.
	/// </summary>
	public static IRuleBuilderOptions<T, string> ApplyPasswordPolicy<T>(this IRuleBuilder<T, string> builder)
	{
		const int min = 8;
		const int max = 128;

		return builder
			.NotEmpty().WithMessage("Password is required")
			.MinimumLength(min).WithMessage($"Password must be at least {min} characters long")
			.MaximumLength(max).WithMessage($"Password must not exceed {max} characters")
			.Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
			.Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
			.Matches("\\d").WithMessage("Password must contain at least one digit")
			.Matches(@"[^\p{L}\p{N}]").WithMessage("Password must contain at least one special character");
	}

	/// <summary>
	/// Applies a zip code format rule (5 digits or 5+4 format) to a rule builder.
	/// </summary>
	public static IRuleBuilderOptions<T, string?> ApplyZipCodePolicy<T>(this IRuleBuilder<T, string?> ruleBuilder, string propertyName = "ZIP code")
	{
		return ruleBuilder
			.NotEmpty().WithMessage($"{propertyName} is required.")
			.Matches(@"^\d{5}(-\d{4})?$").WithMessage($"Invalid {propertyName} format. Must be 5 digits or 5+4 format.");
	}
}
