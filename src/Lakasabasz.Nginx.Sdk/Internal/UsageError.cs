namespace Lakasabasz.Nginx.Sdk.Internal;

/// <summary>An operation the package cannot do as asked, e.g. creating a file that exists; the facades throw for it.</summary>
/// <param name="Kind">What kind of mistake it is, which decides the exception.</param>
/// <param name="Message">What is wrong and what to do instead.</param>
/// <param name="Parameter">The public parameter that is wrong, if one is.</param>
internal sealed record UsageError(UsageErrorKind Kind, string Message, string? Parameter = null);

internal enum UsageErrorKind
{
    /// <summary>The object is not in a state for the operation (<see cref="InvalidOperationException"/>).</summary>
    InvalidOperation,

    /// <summary>An argument is not valid (<see cref="ArgumentException"/>).</summary>
    Argument,
}
