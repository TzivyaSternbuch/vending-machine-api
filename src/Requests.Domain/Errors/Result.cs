using System.Diagnostics.CodeAnalysis;

namespace Requests.Domain.Errors;

/// <summary>
/// Outcome of an operation that can fail in an expected way. Expected failures are returned,
/// never thrown; unexpected failures are ordinary exceptions.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess == (error is not null))
        {
            throw new ArgumentException("A success has no error and a failure must have one.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => !IsSuccess;

    public Error? Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(Error error) => new(false, error);

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(bool isSuccess, T? value, Error? error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>The value of a successful result. Reading it on a failure is a bug and throws.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result has no value.");

    public static Result<T> Success(T value) => new(true, value, null);

    public static new Result<T> Failure(Error error) => new(false, default, error);
}
