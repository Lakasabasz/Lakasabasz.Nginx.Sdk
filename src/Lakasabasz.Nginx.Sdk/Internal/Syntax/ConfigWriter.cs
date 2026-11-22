using System.Text;
using System.Text.RegularExpressions;

namespace Lakasabasz.Nginx.Sdk.Internal.Syntax;

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

/// <summary>Text of changed and new directives.</summary>
internal static class ConfigWriter
{
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
}
