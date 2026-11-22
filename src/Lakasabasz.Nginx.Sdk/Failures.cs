using Lakasabasz.Nginx.Sdk.Internal;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>
/// The package's boundary for errors: internal operations return their failures as results, the public facades turn
/// them into exceptions here.
/// </summary>
internal static class Failures
{
    public static T OrThrow<T>(this Result<T, UsageError> result) =>
        result.IsSuccess ? result.Value : throw ExceptionOf(result.Error);

    public static T OrThrow<T>(this Result<T, NginxParseError> result) =>
        result.IsSuccess ? result.Value : throw new NginxParseException([result.Error]);

    /// <exception cref="NginxParseException">There are errors; it lists all of them.</exception>
    public static void ThrowIfAny(this IReadOnlyList<NginxParseError> errors)
    {
        if (errors.Count > 0)
            throw new NginxParseException(errors);
    }

    private static Exception ExceptionOf(UsageError error) => error.Kind switch
    {
        UsageErrorKind.Argument => new ArgumentException(error.Message, error.Parameter),
        _ => new InvalidOperationException(error.Message),
    };
}
