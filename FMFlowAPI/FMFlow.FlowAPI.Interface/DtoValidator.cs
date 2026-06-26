using FluentValidation;

namespace FMFlow.FlowAPI.Interface;

public static class DtoValidator
{
	public static async Task<Result<T>> Validate<T>(
		T dto,
		IValidator<T> validator,
		CancellationToken ct)
	{
		if (dto == null)
			return Result<T>.Failure("Body can't be null.");

		var validationResult = await validator.ValidateAsync(dto, ct);

		if (!validationResult.IsValid)
		{
			var errorMessage = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
			return Result<T>.Failure(errorMessage);
		}

		return Result<T>.Success(dto);
	}
}
