namespace Lakasabasz.Nginx.Sdk;

/// <summary>A syntax error in a config file.</summary>
/// <param name="File">The file with the error.</param>
/// <param name="Line">1-based line of the error.</param>
/// <param name="Column">1-based column of the error.</param>
/// <param name="Message">What is wrong, in nginx's words, e.g. <c>unexpected "}"</c>.</param>
public sealed record NginxParseError(string File, int Line, int Column, string Message);

/// <summary>
/// Loading failed: at least one config file has a syntax error. Every file is parsed before this is thrown, so
/// <see cref="Errors"/> lists every broken file (one error per file: parsing a file stops at its first error).
/// </summary>
public sealed class NginxParseException : NginxException
{
    /// <summary>Creates the exception.</summary>
    public NginxParseException(IReadOnlyList<NginxParseError> errors)
        : base(Describe(errors))
    {
        Errors = errors;
    }

    /// <summary>The syntax errors, at most one per file, in loading order.</summary>
    public IReadOnlyList<NginxParseError> Errors { get; }

    private static string Describe(IReadOnlyList<NginxParseError> errors) =>
        "Cannot load the nginx config:" + string.Concat(errors.Select(e => $"{Environment.NewLine}{e.File}:{e.Line}:{e.Column}: {e.Message}"));
}
