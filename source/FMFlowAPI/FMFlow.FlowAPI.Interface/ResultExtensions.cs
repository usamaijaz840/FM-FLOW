using FluentValidation;
using Microsoft.Extensions.Logging;

namespace FMFlow.FlowAPI.Interface;

public static class ResultExtensions
{
	public static async Task<Result<T>> ValidateWithResult<T>(this IValidator<T> validator, T dto, CancellationToken ct, string? dtoTypeName = null)
	{
		ct.ThrowIfCancellationRequested();

		if (dto is null)
		{
			return Result<T>.Failure($"{dtoTypeName ?? typeof(T).Name} cannot be null.");
		}

		var validationResult = await validator.ValidateAsync(dto, ct);

		if (!validationResult.IsValid)
		{
			var errorMessage = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
			return Result<T>.Failure(errorMessage);
		}

		return Result<T>.Success(dto);
	}

	public static async Task<Result<T>> ValidateResult<T>(this Result<T> result, IValidator<T> validator, CancellationToken ct, string? dtoTypeName = null)
	{
		ct.ThrowIfCancellationRequested();

		return await result.MapResult(async (T dto, CancellationToken currentCancellationToken) =>
			await validator.ValidateWithResult(dto, currentCancellationToken, dtoTypeName), ct);
	}

	public static async Task<Result<T>> ValidateResult<T>(this Task<Result<T>> result, IValidator<T> validator, string? dtoTypeName, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		var initialResult = await result;
		return await initialResult.ValidateResult(validator, ct, dtoTypeName);
	}

	public static Result<R> MapResult<T, R>(this Result<T> result, Func<T, Result<R>> mapFunc)
	{
		if (result.IsSuccess)
		{
			return mapFunc(result.Value!);
		}

		return Result<R>.Failure(result.Error!, result.ErrorType);
	}

	public static async Task<Result<R>> MapResult<T, R>(this Result<T> result, Func<T, CancellationToken, Task<Result<R>>> mapFunc, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		if (result.IsSuccess)
		{
			return await mapFunc(result.Value!, ct);
		}

		return Result<R>.Failure(result.Error!, result.ErrorType);
	}

	public static async Task<Result<R>> MapResult<T, R>(this Task<Result<T>> resultTask, Func<T, Result<R>> mapFunc, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		var initialResult = await resultTask;

		return MapResult(initialResult, mapFunc);
	}

	public static async Task<Result<R>> MapResult<T, R>(this Task<Result<T>> resultTask, Func<T, CancellationToken, Task<Result<R>>> mapFunc, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		var initialResult = await resultTask;

		return await MapResult(initialResult, mapFunc, ct);
	}

	public static Result MapResult<T>(this Result<T> result, Func<T, Result> mapFunc)
	{
		if (result.IsSuccess)
		{
			return mapFunc(result.Value!);
		}

		return Result.Failure(result.Error!, result.ErrorType);
	}

	public static async Task<Result> MapResult<T>(this Result<T> result, Func<T, CancellationToken, Task<Result>> mapFunc, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		if (result.IsSuccess)
		{
			return await mapFunc(result.Value!, ct);
		}

		return Result.Failure(result.Error!, result.ErrorType);
	}

	public static async Task<Result> MapResult<T>(this Task<Result<T>> resultTask, Func<T, Result> mapFunc, CancellationToken ct)
	{
		var initialResult = await resultTask;

		return MapResult(initialResult, mapFunc);
	}

	public static async Task<Result> MapResult<T>(this Task<Result<T>> resultTask, Func<T, CancellationToken, Task<Result>> mapFunc, CancellationToken ct)
	{
		var initialResult = await resultTask;

		return await MapResult(initialResult, mapFunc, ct);
	}

	public static Result<R> MapResult<R>(this Result result, Func<Result<R>> mapFunc)
	{
		if (result.IsSuccess)
		{
			return mapFunc();
		}

		return Result<R>.Failure(result.Error!, result.ErrorType);
	}

	public static async Task<Result<R>> MapResult<R>(this Result result, Func<CancellationToken, Task<Result<R>>> mapFunc, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		if (result.IsSuccess)
		{
			return await mapFunc(ct);
		}

		return Result<R>.Failure(result.Error!, result.ErrorType);
	}

	public static async Task<Result<R>> MapResult<R>(this Task<Result> resultTask, Func<Result<R>> mapFunc, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		var initialResult = await resultTask;

		return MapResult(initialResult, mapFunc);
	}

	public static async Task<Result<R>> MapResult<R>(this Task<Result> resultTask, Func<CancellationToken, Task<Result<R>>> mapFunc, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		var initialResult = await resultTask;

		return await MapResult(initialResult, mapFunc, ct);
	}
	public static async Task<Result> ToResult<T>(this Task<Result<T>> resultTask, CancellationToken ct)
	{
		Result<T> result = await resultTask;
		if (result.IsSuccess)
			return Result.Success();

		return Result.Failure(result.Error!, result.ErrorType);
	}

	public static Result MapResult<T>(this Result<T> result)
	{
		if (result.IsSuccess)
			return Result.Success();

		return Result.Failure(result.Error!, result.ErrorType);
	}

	public static Result MergeResult(this Result result, Func<Result> mapFunc)
	{
		if (result.IsSuccess)
		{
			return mapFunc();
		}

		return Result.Failure(result.Error!, result.ErrorType);
	}

	public static async Task<Result> MergeResult(this Result result, Func<CancellationToken, Task<Result>> mapFunc, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		if (result.IsSuccess)
		{
			return await mapFunc(ct);
		}

		return Result.Failure(result.Error!, result.ErrorType);
	}

	public static async Task<Result> MergeResult(this Task<Result> resultTask, Func<Result> mapFunc, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		var initialResult = await resultTask;

		return MergeResult(initialResult, mapFunc);
	}

	public static async Task<Result> MergeResult(this Task<Result> resultTask, Func<CancellationToken, Task<Result>> mapFunc, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		var initialResult = await resultTask;

		return await MergeResult(initialResult, mapFunc, ct);
	}

	/// <summary>
	/// Log any errors that the result has. This is useful in cases such as notifications where failure to send should not fail the main operation,
	/// but should still be logged for further investigation.
	/// </summary>
	/// <param name="result">The result to log any errors for.</param>
	/// <param name="logger">The logger to use for logging.</param>
	public static void LogAnyErrors(this Result result, ILogger logger)
	{
		if (!result.IsSuccess)
			logger.LogError("Operation failed with error type {ErrorType}: {Error}", result.ErrorType, result.Error);
	}
}
