using System.Text;
using System.Text.RegularExpressions;

namespace Lakasabasz.Nginx.Sdk.Internal;

/// <summary>Line break and indentation unit of a file, used for the text of new directives.</summary>
internal sealed record Formatting(string Newline, string IndentUnit)
{
    public static Formatting Default { get; } = new("\n", "    ");

    /// <summary>The dominant line break and the indentation unit (tab or four spaces) of <paramref name="text"/>.</summary>
    public static Formatting Of(string text)
    {
        var crlf = Regex.Matches(text, "\r\n").Count;
        var lf = Regex.Matches(text, "(?<!\r)\n").Count;
        var tabs = Regex.Matches(text, "\n\t").Count;
        var spaces = Regex.Matches(text, "\n ").Count;
        return new Formatting(crlf > lf ? "\r\n" : "\n", tabs > spaces ? "\t" : "    ");
    }
}

/// <summary>Text of changed and new directives, and trimming around removed ones.</summary>
internal static class ConfigWriter
{
    private static readonly Regex RestOfLine = new(@"^[ \t]*(#[^\n]*)?(\r?\n)?", RegexOptions.CultureInvariant);
    private static readonly Regex TrailingComment = new(@"^[ \t]*(#[^\r\n]*)?", RegexOptions.CultureInvariant);

    /// <summary><c>name arg1 arg2</c>, plus a space before <c>{</c> for a block.</summary>
    public static void AppendHeader(StringBuilder output, string name, IReadOnlyList<string> arguments, bool block)
    {
        output.Append(Quote(name));
        foreach (var argument in arguments)
            output.Append(' ').Append(Quote(argument));
        if (block)
            output.Append(' ');
    }

    /// <summary>The argument as nginx needs it written: as is, or double-quoted with <c>"</c> and <c>\</c> escaped.</summary>
    public static string Quote(string value)
    {
        var plain = value.Length > 0 && value[0] != '#'
            && value.All(c => !char.IsWhiteSpace(c) && c is not ('"' or '\'' or ';' or '{' or '}' or '\\'));
        if (plain)
            return value;

        var quoted = new StringBuilder("\"");
        foreach (var c in value)
        {
            if (c is '"' or '\\')
                quoted.Append('\\');
            quoted.Append(c);
        }
        return quoted.Append('"').ToString();
    }

    /// <summary>
    /// The text before a removed directive without the directive's indentation, and whether the directive started
    /// its line (only spaces and tabs between the last line break and the directive).
    /// </summary>
    public static (string Kept, bool StartsLine) WithoutIndentation(string before)
    {
        var lineStart = before.LastIndexOf('\n');
        var indentation = before.Substring(lineStart + 1);
        return indentation.All(c => c is ' ' or '\t') && (lineStart >= 0 || before.Length == indentation.Length)
            ? (before.Substring(0, lineStart + 1), true)
            : (before, false);
    }

    /// <summary>
    /// The text after a removed directive without the rest of its line: a comment on the same line and, when the
    /// directive had the line to itself, the line break.
    /// </summary>
    public static string WithoutRestOfLine(string after, bool startsLine) =>
        after.Substring((startsLine ? RestOfLine : TrailingComment).Match(after).Length);
}
