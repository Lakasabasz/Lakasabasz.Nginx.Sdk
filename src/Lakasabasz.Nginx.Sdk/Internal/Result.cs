using System.Diagnostics.CodeAnalysis;

namespace Lakasabasz.Nginx.Sdk.Internal;

/// <summary>
/// The outcome of an internal operation whose failure is an expected result, not an exceptional one: a value or an
/// error. Public methods turn an error into an exception at the package's boundary.
/// </summary>
internal sealed class Result<TValue, TError>
    where TValue : class
    where TError : class
{
    private Result(TValue? value, TError? error)
    {
        Value = value;
        Error = error;
    }

    /// <summary>The value; <see langword="null"/> when the operation failed.</summary>
    public TValue? Value { get; }

    /// <summary>The error; <see langword="null"/> when the operation succeeded.</summary>
    public TError? Error { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Value is not null;

    public static Result<TValue, TError> Success(TValue value) => new(value, null);

    public static Result<TValue, TError> Failure(TError error) => new(null, error);
}
