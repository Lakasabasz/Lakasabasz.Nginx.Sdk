namespace Lakasabasz.Nginx.Sdk.Internal.Syntax;

/// <summary>Line break and indentation unit of a file, used for the text of new directives.</summary>
internal sealed record Formatting(string Newline, string IndentUnit)
{
    private const string Lf = "\n";
    private const string Crlf = "\r\n";
    private const string Tab = "\t";
    private const string FourSpaces = "    ";

    public static Formatting Default { get; } = new(Lf, FourSpaces);

    /// <summary>A formatting set in code: the line break is <c>\n</c> or <c>\r\n</c>, the indentation any spaces and tabs.</summary>
    public static Result<Formatting, UsageError> Create(string newline, string indentUnit)
    {
        if (newline is not (Lf or Crlf))
            return Result<Formatting, UsageError>.Failure(new UsageError(UsageErrorKind.Argument, "The line break is \\n or \\r\\n.", "value"));
        if (indentUnit.Length == 0 || indentUnit.Any(c => c is not (' ' or '\t')))
            return Result<Formatting, UsageError>.Failure(new UsageError(UsageErrorKind.Argument, "The indentation is spaces or tabs.", "value"));
        return Result<Formatting, UsageError>.Success(new Formatting(newline, indentUnit));
    }

    /// <summary>
    /// The formatting of <paramref name="text"/>: the more common line break (LF on a tie), and a tab when more lines
    /// start with a tab than with a space, else four spaces (the unit is not measured, only its kind).
    /// </summary>
    public static Formatting Of(string text)
    {
        var lines = text.Split('\n');
        var broken = lines.Take(lines.Length - 1).ToList();
        var crlf = broken.Count(line => line.EndsWith("\r", StringComparison.Ordinal));
        var afterBreak = lines.Skip(1).ToList();
        var tabs = afterBreak.Count(line => line.StartsWith(Tab, StringComparison.Ordinal));
        var spaces = afterBreak.Count(line => line.StartsWith(" ", StringComparison.Ordinal));
        return new Formatting(crlf > broken.Count - crlf ? Crlf : Lf, tabs > spaces ? Tab : FourSpaces);
    }
}
