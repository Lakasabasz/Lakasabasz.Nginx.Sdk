using System.Text;

namespace Lakasabasz.Nginx.Sdk.Internal.Syntax;

/// <summary>Words of a directive: quoted, plain, with variables and escapes, as nginx reads them.</summary>
internal sealed partial class ConfigParser
{
    private const char Escape = '\\';

    private string ReadWord() => _text.Current is '"' or '\'' ? ReadQuotedWord() : ReadPlainWord();

    /// <summary>A word in quotes: up to the same quote, then whitespace, <c>;</c>, <c>{</c> or <c>)</c> must follow.</summary>
    private string ReadQuotedWord()
    {
        var quote = _text.Current;
        _text.Advance();
        var start = _text.Position;
        while (true)
        {
            if (_text.AtEnd)
                throw new SyntaxError(_text.Position, EndOfFileInDirective);
            if (_text.Current == Escape)
                _text.Advance(2);
            else if (_text.Current == quote)
                break;
            else
                _text.Advance();
        }

        var word = Unescape(start, _text.Position);
        _text.Advance();
        if (!_text.AtEnd && !CanFollowQuote(_text.Current))
            throw Unexpected();
        return word;
    }

    /// <summary>A word without quotes: up to whitespace, <c>;</c> or <c>{</c>; an escaped character and the <c>{</c> of <c>${</c> belong to it.</summary>
    private string ReadPlainWord()
    {
        var start = _text.Position;
        while (!_text.AtEnd && !EndsPlainWord(_text.Current))
            _text.Advance(_text.Current == Escape || StartsVariableName() ? 2 : 1);
        return Unescape(start, Math.Min(_text.Position, _text.Text.Length));
    }

    private bool StartsVariableName() => _text.Current == '$' && _text.Peek(1) == '{';

    private static bool EndsPlainWord(char c) => IsSpace(c) || c is ';' or '{';

    private static bool CanFollowQuote(char c) => IsSpace(c) || c is ';' or '{' or ')';

    /// <summary>The value of a word as nginx stores it (ngx_conf_read_token's unescaping).</summary>
    private string Unescape(int start, int end)
    {
        var value = new StringBuilder(end - start);
        var i = start;
        while (i < end)
        {
            var c = _text.Text[i];
            if (c == Escape && i + 1 < end && EscapedCharacter(_text.Text[i + 1]) is { } escaped)
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
