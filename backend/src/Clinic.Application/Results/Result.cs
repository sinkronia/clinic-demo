namespace Clinic.Application.Results;

public enum ErrorKind
{
    Validation,
    NotFound,
    Conflict,
}

/// <summary>A business error. <see cref="Code"/> is a stable machine-readable contract (BE-009), never user-facing text.</summary>
public sealed record BusinessError(
    string Code,
    ErrorKind Kind,
    IReadOnlyDictionary<string, string[]>? FieldErrors = null);

public sealed class Result<T>
{
    internal Result(T? value, BusinessError? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }

    public BusinessError? Error { get; }

    public bool IsSuccess => Error is null;
}

public static class Result
{
    public static Result<T> Success<T>(T value) => new(value, null);

    public static Result<T> Failure<T>(BusinessError error) => new(default, error);
}
