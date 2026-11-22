using System.Diagnostics.CodeAnalysis;

namespace Lakasabasz.Nginx.Sdk.Internal;

/// <summary>
/// The outcome of an internal operation whose failure is an expected result, not an exceptional one: a value or an
/// error. Internal code never throws for such a failure; the public facades turn the error into an exception.
/// </summary>
internal sealed class Result<TValue, TError>
    where TError : class
{
    private Result(bool isSuccess, TValue value, TError? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    /// <summary>The value; the type's default when the operation failed.</summary>
    public TValue Value { get; }

    /// <summary>The error; <see langword="null"/> when the operation succeeded.</summary>
    public TError? Error { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    public static Result<TValue, TError> Success(TValue value) => new(true, value, null);

    public static Result<TValue, TError> Failure(TError error) => new(false, default!, error);
}
