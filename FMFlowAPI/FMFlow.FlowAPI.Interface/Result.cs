namespace FMFlow.FlowAPI.Interface;

public record Result(bool IsSuccess, string? Error = null, ResultErrorType? ErrorType = null)
{
	public static Result Success() => new(true, null, null);

	public static Result Failure(string error, ResultErrorType? errorType = ResultErrorType.BadRequest) =>
		new(false, error, errorType);
}

public record Result<T>(bool IsSuccess, T? Value = default, string? Error = null, ResultErrorType? ErrorType = null) : Result(IsSuccess, Error, ErrorType)
{
	public static Result<T> Success(T value) => new(true, value, null, null);

	public new static Result<T> Failure(string error, ResultErrorType? errorType = ResultErrorType.BadRequest) =>
		new(false, default, error, errorType);
}

public enum ResultErrorType
{
	BadRequest,
	NotFound,
	PermissionDenied,
	Unauthorized
}
