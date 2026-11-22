namespace Lakasabasz.Nginx.Sdk;

/// <summary>A syntax error in a config file.</summary>
/// <param name="File">The file with the error.</param>
/// <param name="Line">1-based line of the error.</param>
/// <param name="Column">1-based column of the error.</param>
/// <param name="Message">What is wrong, in nginx's words, e.g. <c>unexpected "}"</c>.</param>
public sealed record NginxParseError(NginxPath File, int Line, int Column, string Message)
{
    /// <summary>A missing file, reported as nginx does: at the place that names it, or at the file itself.</summary>
    /// <param name="missing">The file that does not exist.</param>
    /// <param name="namedIn">The file naming it (an <c>include</c>); the missing file itself when nothing names it.</param>
    /// <param name="line">1-based line naming it; 0 when nothing names it.</param>
    /// <param name="column">1-based column naming it; 0 when nothing names it.</param>
    internal static NginxParseError NotFound(NginxPath missing, NginxPath? namedIn = null, int line = 0, int column = 0) =>
        new(namedIn ?? missing, line, column, $"open() \"{missing}\" failed (2: No such file or directory)");
}

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
