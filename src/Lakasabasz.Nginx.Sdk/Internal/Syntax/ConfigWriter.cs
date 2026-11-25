using System.Text;

namespace Lakasabasz.Nginx.Sdk.Internal.Syntax;

/// <summary>Text of changed and new directives: their name and arguments, quoted where nginx needs it.</summary>
internal static class ConfigWriter
{
    /// <summary>The quote written around a value that needs quoting.</summary>
    private const char QuoteMark = '"';

    /// <summary>The character written before a quote or backslash inside quotes.</summary>
    private const char EscapeMark = '\\';

    /// <summary><c>name arg1 arg2</c>, without the <c>;</c> or <c>{</c> that ends it.</summary>
    public static void AppendHeader(StringBuilder output, string name, IReadOnlyList<string> arguments)
    {
        output.Append(Quote(name));
        foreach (var argument in arguments)
            output.Append(' ').Append(Quote(argument));
    }

    /// <summary>The argument as nginx needs it written: as is, or double-quoted with <c>"</c> and <c>\</c> escaped.</summary>
    public static string Quote(string value)
    {
        if (!NeedsQuotes(value))
            return value;

        var quoted = new StringBuilder().Append(QuoteMark);
        foreach (var c in value)
        {
            if (c == QuoteMark || c.IsEscape())
                quoted.Append(EscapeMark);
            quoted.Append(c);
        }
        return quoted.Append(QuoteMark).ToString();
    }

    /// <summary>
    /// Whether nginx would read the value differently unquoted: empty, starting a comment (<c>#</c>), or with
    /// whitespace (line breaks too), a quote, <c>;</c>, <c>{</c>, <c>}</c> or a backslash.
    /// </summary>
    private static bool NeedsQuotes(string value) => value.Length == 0 || value[0].IsCommentStart()
        || value.Any(c => char.IsWhiteSpace(c) || c.IsQuote() || c.IsDirectiveEnd() || c.IsBlockBegin() || c.IsBlockEnd() || c.IsEscape());
}
