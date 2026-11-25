using System.Text;

namespace Lakasabasz.Nginx.Sdk.Internal.Syntax;

/// <summary>Words of a directive: quoted, plain, with variables and escapes, as nginx reads them.</summary>
internal static partial class ConfigParser
{
    private static Result<string, SyntaxProblem> ParseWord(TextCursor cursor) => cursor.Current.IsQuote() ? ParseQuotedWord(cursor) : Success(ParsePlainWord(cursor));

    /// <summary>A word in quotes: up to the same quote, then whitespace, <c>;</c>, <c>{</c> or <c>)</c> must follow.</summary>
    private static Result<string, SyntaxProblem> ParseQuotedWord(TextCursor cursor)
    {
        var quote = cursor.Current;
        cursor.Advance();
        var start = cursor.CurrentPosition;
        while (!cursor.AtEnd && cursor.Current != quote)
            cursor.Advance(cursor.Current.IsEscape() ? 2 : 1);
        if (cursor.AtEnd)
            return Failure<string>(Problem(cursor, EndOfFileInDirective));

        var word = Unescape(cursor, start.Index, cursor.Index);
        cursor.Advance();
        if (!cursor.AtEnd && !CanFollowQuote(cursor.Current))
            return Failure<string>(Unexpected(cursor));
        return Success(word);
    }

    /// <summary>A word without quotes: up to whitespace, <c>;</c> or <c>{</c>; an escaped character and the <c>{</c> of <c>${</c> belong to it.</summary>
    private static string ParsePlainWord(TextCursor cursor)
    {
        var start = cursor.CurrentPosition;
        while (!cursor.AtEnd && !EndsPlainWord(cursor.Current))
            cursor.Advance(cursor.Current.IsEscape() || StartsVariableName(cursor) ? 2 : 1);
        return Unescape(cursor, start.Index, Math.Min(cursor.Index, cursor.Text.Length));
    }

    private static bool StartsVariableName(TextCursor cursor) => cursor.Current == '$' && cursor.Peek(1) == '{';

    private static bool EndsPlainWord(char c) => c.IsSpace() || c.IsDirectiveEnd() || c.IsBlockBegin();

    private static bool CanFollowQuote(char c) => c.IsSpace() || c.IsDirectiveEnd() || c.IsBlockBegin() || c == ')';

    /// <summary>The value of a word as nginx stores it (ngx_conf_read_token's unescaping).</summary>
    private static string Unescape(TextCursor cursor, int start, int end)
    {
        var value = new StringBuilder(end - start);
        var i = start;
        while (i < end)
        {
            var c = cursor.Text[i];
            if (c.IsEscape() && i + 1 < end && EscapedCharacter(cursor.Text[i + 1]) is { } escaped)
            {
                value.Append(escaped);
                i += 2;
            }
            else
            {
                value.Append(c);
                i++;
            }
        }
        return value.ToString();
    }

    /// <summary>The character an escape stands for; <see langword="null"/> when the backslash stays as written.</summary>
    private static char? EscapedCharacter(char c) => c switch
    {
        '"' or '\'' or '\\' => c,
        't' => '\t',
        'r' => '\r',
        'n' => '\n',
        _ => null,
    };
}
