using System.Text;

namespace Lakasabasz.Nginx.Sdk.Internal;

/// <summary>
/// Lossless parser of nginx config files: keeps every character, so an unchanged file is written back as it was.
/// Follows nginx's own tokenizer (ngx_conf_read_token): the same tokens, escapes and syntax errors.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Tokens are separated by whitespace, <c>;</c> and <c>{</c>; <c>}</c> only ends a block at the start of a token.</item>
/// <item><c>#</c> starts a comment only at the start of a token.</item>
/// <item>In an unquoted word, <c>${</c> starts a variable name, not a block.</item>
/// <item>After a closing quote comes whitespace, <c>;</c>, <c>{</c> or <c>)</c>.</item>
/// <item>A backslash escapes the next character; <c>\"</c>, <c>\'</c> and <c>\\</c> become the character,
/// <c>\t</c>, <c>\r</c> and <c>\n</c> the control character, others keep the backslash.</item>
/// <item>A raw block (<c>*_by_lua_block</c> of OpenResty) keeps its body as text, read up to the matching <c>}</c>
/// outside Lua strings and comments. (njs has no inline code in the config: it loads files with <c>js_import</c>.)</item>
/// </list>
/// Parsing stops at the first error of a file, as nginx does.
/// </remarks>
internal sealed class ConfigParser
{
    private readonly string _file;
    private readonly string _text;
    private readonly int[] _lineStarts;
    private readonly Formatting _formatting;
    private int _position;

    private ConfigParser(string file, string text)
    {
        _file = file;
        _text = text;
        _formatting = Formatting.Of(text);
        var lineStarts = new List<int> { 0 };
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
                lineStarts.Add(i + 1);
        }
        _lineStarts = lineStarts.ToArray();
    }

    /// <summary>The top-level directives of the file, or its syntax error.</summary>
    public static (DirectiveCollection? Directives, NginxParseError? Error) Parse(string file, string text)
    {
        var parser = new ConfigParser(file, text);
        try
        {
            return (parser.ParseCollection(block: false), null);
        }
        catch (SyntaxError error)
        {
            var (line, column) = parser.LineAndColumn(error.Position);
            return (null, new NginxParseError(file, line, column, error.Message));
        }
    }

    private DirectiveCollection ParseCollection(bool block)
    {
        var items = new List<object>();
        while (true)
        {
            var triviaStart = _position;
            SkipTrivia();
            items.Add(_text.Substring(triviaStart, _position - triviaStart));

            if (_position >= _text.Length)
            {
                if (block)
                    throw new SyntaxError(_position, "unexpected end of file, expecting \"}\"");
                return new DirectiveCollection(items, _formatting);
            }

            switch (_text[_position])
            {
                case '}':
                    if (!block)
                        throw new SyntaxError(_position, "unexpected \"}\"");
                    _position++;
                    return new DirectiveCollection(items, _formatting);
                case ';':
                    throw new SyntaxError(_position, "unexpected \";\"");
                case '{':
                    throw new SyntaxError(_position, "unexpected \"{\"");
                default:
                    items.Add(ParseDirective());
                    break;
            }
        }
    }

    private Directive ParseDirective()
    {
        var start = _position;
        var (line, column) = LineAndColumn(start);
        var words = new List<string>();
        while (true)
        {
            words.Add(ReadWord());
            SkipTrivia();
            if (_position >= _text.Length)
                throw new SyntaxError(_position, "unexpected end of file, expecting \";\" or \"}\"");

            var c = _text[_position];
            if (c is ';' or '{')
            {
                var header = _text.Substring(start, _position - start);
                _position++;
                var name = words[0];
                var arguments = words.Skip(1).ToArray();
                if (c == ';')
                    return new Directive(name, arguments, DirectiveKind.Simple, header, line, column, null, null);
                if (IsRawBlock(name))
                    return new Directive(name, arguments, DirectiveKind.Raw, header, line, column, null, ReadRawBody());
                return new Directive(name, arguments, DirectiveKind.Block, header, line, column, ParseCollection(block: true), null);
            }
            if (c == '}')
                throw new SyntaxError(_position, "unexpected \"}\"");
        }
    }

    /// <summary>Whitespace and comments.</summary>
    private void SkipTrivia()
    {
        while (_position < _text.Length)
        {
            var c = _text[_position];
            if (c == '#')
            {
                while (_position < _text.Length && _text[_position] != '\n')
                    _position++;
            }
            else if (IsSpace(c))
            {
                _position++;
            }
            else
            {
                return;
            }
        }
    }

    private string ReadWord()
    {
        var start = _position;
        var quote = _text[_position];
        if (quote is '"' or '\'')
        {
            _position++;
            while (true)
            {
                if (_position >= _text.Length)
                    throw new SyntaxError(_position, "unexpected end of file, expecting \";\" or \"}\"");
                var c = _text[_position];
                if (c == '\\')
                {
                    _position += 2;
                    continue;
                }
                _position++;
                if (c == quote)
                    break;
            }
            if (_position < _text.Length && !IsSpace(_text[_position]) && _text[_position] is not (';' or '{' or ')'))
                throw new SyntaxError(_position, $"unexpected \"{_text[_position]}\"");
            return Unescape(start + 1, _position - 1);
        }

        var variable = false;
        while (_position < _text.Length)
        {
            var c = _text[_position];
            if (c == '\\')
            {
                _position += 2;
                variable = false;
                continue;
            }
            if (c == '$')
            {
                variable = true;
                _position++;
                continue;
            }
            if (c == '{' && variable)
            {
                _position++;
                continue;
            }
            if (IsSpace(c) || c is ';' or '{')
                break;
            variable = false;
            _position++;
        }
        return Unescape(start, Math.Min(_position, _text.Length));
    }

    /// <summary>The value of a token as nginx stores it (ngx_conf_read_token's unescaping).</summary>
    private string Unescape(int start, int end)
    {
        var value = new StringBuilder(end - start);
        for (var i = start; i < end; i++)
        {
            var c = _text[i];
            if (c == '\\' && i + 1 < end)
            {
                var next = _text[i + 1];
                switch (next)
                {
                    case '"' or '\'' or '\\':
                        value.Append(next);
                        i++;
                        continue;
                    case 't':
                        value.Append('\t');
                        i++;
                        continue;
                    case 'r':
                        value.Append('\r');
                        i++;
                        continue;
                    case 'n':
                        value.Append('\n');
                        i++;
                        continue;
                }
            }
            value.Append(c);
        }
        return value.ToString();
    }

    /// <summary>The body of a raw block, up to its closing <c>}</c> (consumed), skipping Lua strings and comments.</summary>
    private string ReadRawBody()
    {
        var start = _position;
        var depth = 0;
        while (_position < _text.Length)
        {
            var c = _text[_position];
            if (c is '"' or '\'')
            {
                SkipLuaString(c);
                continue;
            }
            if (c == '-' && Peek(1) == '-')
            {
                _position += 2;
                if (!SkipLuaLongBracket())
                {
                    while (_position < _text.Length && _text[_position] != '\n')
                        _position++;
                }
                continue;
            }
            if (c == '[' && SkipLuaLongBracket())
                continue;
            if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                if (depth == 0)
                {
                    var body = _text.Substring(start, _position - start);
                    _position++;
                    return body;
                }
                depth--;
            }
            _position++;
        }
        throw new SyntaxError(_position, "unexpected end of file, expecting \"}\"");
    }

    private void SkipLuaString(char quote)
    {
        _position++;
        while (_position < _text.Length && _text[_position] != quote && _text[_position] != '\n')
            _position += _text[_position] == '\\' ? 2 : 1;
        _position++;
    }

    /// <summary>Skips a Lua long bracket (<c>[[ … ]]</c>, <c>[==[ … ]==]</c>) starting at the position, if there is one.</summary>
    private bool SkipLuaLongBracket()
    {
        if (Peek(0) != '[')
            return false;
        var level = 0;
        while (Peek(1 + level) == '=')
            level++;
        if (Peek(1 + level) != '[')
            return false;

        var close = "]" + new string('=', level) + "]";
        var end = _text.IndexOf(close, _position + level + 2, StringComparison.Ordinal);
        _position = end < 0 ? _text.Length : end + close.Length;
        return true;
    }

    private char Peek(int offset) => _position + offset < _text.Length ? _text[_position + offset] : '\0';

    private static bool IsRawBlock(string name) => name.EndsWith("_by_lua_block", StringComparison.Ordinal);

    private static bool IsSpace(char c) => c is ' ' or '\t' or '\r' or '\n';

    private (int Line, int Column) LineAndColumn(int position)
    {
        var index = Array.BinarySearch(_lineStarts, position);
        var line = index >= 0 ? index : ~index - 1;
        return (line + 1, position - _lineStarts[line] + 1);
    }

    private sealed class SyntaxError(int position, string message) : Exception(message)
    {
        public int Position { get; } = position;
    }
}
