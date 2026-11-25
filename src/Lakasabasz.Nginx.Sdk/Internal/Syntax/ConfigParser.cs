namespace Lakasabasz.Nginx.Sdk.Internal.Syntax;

/// <summary>
/// Lossless parser of nginx config files into a syntax tree, which keeps every character (lines, indentation,
/// comments), so an unchanged file is written back as it was.
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
/// Parsing stops at the first error of a file, as nginx does. This file reads lines and directives; words are in
/// ConfigParser.Words.cs, raw Lua blocks in ConfigParser.Lua.cs.
/// </remarks>
internal sealed partial class ConfigParser
{
    private const string EndOfFileInBlock = "unexpected end of file, expecting \"}\"";
    private const string EndOfFileInDirective = "unexpected end of file, expecting \";\" or \"}\"";

    private readonly TextCursor _text;

    private ConfigParser(string text)
    {
        _text = new TextCursor(text);
    }

    /// <summary>The syntax tree of the file, or its syntax error.</summary>
    public static Result<SyntaxTree, NginxParseError> Parse(NginxPath file, string text)
    {
        var parser = new ConfigParser(text);
        try
        {
            return Result<SyntaxTree, NginxParseError>.Success(new SyntaxTree(parser.ParseFile(), text));
        }
        catch (SyntaxError error)
        {
            var (line, column) = parser._text.LineAndColumn(error.Position);
            return Result<SyntaxTree, NginxParseError>.Failure(new NginxParseError(file, line, column, error.Message));
        }
    }

    private SyntaxBlock ParseFile() => FileOf(ParseLines(insideBlock: false));

    /// <summary>The lines of a file, or of a block up to its closing <c>}</c> (consumed).</summary>
    private List<SyntaxLine> ParseLines(bool insideBlock)
    {
        var lines = new List<SyntaxLine>();
        var line = new SyntaxLine { Indentation = ReadBlanks() };
        while (true)
        {
            var spacing = ReadBlanks();
            if (_text.AtEnd)
            {
                if (insideBlock)
                    throw new SyntaxError(_text.Position, EndOfFileInBlock);
                lines.Add(Finish(line, spacing, ending: string.Empty));
                return lines;
            }

            if (_text.AtLineBreak)
            {
                lines.Add(Finish(line, spacing, ReadLineBreak()));
                line = new SyntaxLine { Indentation = ReadBlanks() };
                continue;
            }

            switch (_text.Current)
            {
                case '}' when insideBlock:
                    _text.Advance();
                    lines.Add(Finish(line, spacing, ending: string.Empty));
                    return lines;
                case '}' or ';' or '{':
                    throw Unexpected();
                case '#':
                    line.Items.Add(new SyntaxItem(spacing, ReadComment()));
                    break;
                default:
                    line.Items.Add(new SyntaxItem(spacing, ParseDirective()));
                    break;
            }
        }
    }

    private SyntaxDirective ParseDirective()
    {
        var start = _text.Position;
        var (line, column) = _text.LineAndColumn(start);
        var words = ReadWordsOfDirective();
        var header = _text.Take(start);
        var endsWithBlock = _text.Current == '{';
        _text.Advance();

        var name = words[0];
        var arguments = words.Skip(1).ToArray();
        if (!endsWithBlock)
            return new SyntaxDirective(name, arguments, header, null, null, line, column);
        if (IsRawBlock(name))
            return new SyntaxDirective(name, arguments, header, null, ReadRawBody(), line, column);
        return new SyntaxDirective(name, arguments, header, BlockOf(ParseLines(insideBlock: true)), null, line, column);
    }

    /// <summary>The name and arguments of a directive, up to its <c>;</c> or <c>{</c> (not consumed).</summary>
    private List<string> ReadWordsOfDirective()
    {
        var words = new List<string>();
        do
        {
            words.Add(ReadWord());
            SkipSpaceAndComments();
            if (_text.AtEnd)
                throw new SyntaxError(_text.Position, EndOfFileInDirective);
            if (_text.Current == '}')
                throw Unexpected();
        }
        while (_text.Current is not (';' or '{'));
        return words;
    }

    /// <summary>Whitespace, line breaks and comments between the words of a directive.</summary>
    private void SkipSpaceAndComments()
    {
        while (!_text.AtEnd)
        {
            if (_text.Current == '#')
                SkipToLineEnd();
            else if (IsSpace(_text.Current))
                _text.Advance();
            else
                return;
        }
    }

    /// <summary>Spaces and tabs (and a <c>\r</c> not starting a line break): the spacing on a line.</summary>
    private string ReadBlanks()
    {
        var start = _text.Position;
        while (!_text.AtEnd && (_text.Current is ' ' or '\t' || _text.Current == '\r' && !_text.AtLineBreak))
            _text.Advance();
        return _text.Take(start);
    }

    private string ReadLineBreak()
    {
        var start = _text.Position;
        _text.Advance(_text.LineBreakLength);
        return _text.Take(start);
    }

    private SyntaxComment ReadComment()
    {
        var start = _text.Position;
        while (!_text.AtEnd && !_text.AtLineBreak)
            _text.Advance();
        return new SyntaxComment(_text.Take(start));
    }

    /// <summary>Up to the <c>\n</c> ending the line (not consumed).</summary>
    private void SkipToLineEnd()
    {
        while (!_text.AtEnd && _text.Current != '\n')
            _text.Advance();
    }

    private static SyntaxLine Finish(SyntaxLine line, string trailing, string ending)
    {
        line.Trailing = trailing;
        line.Ending = ending;
        return line;
    }

    /// <summary>The lines of a file; the empty rest after the last line break is not a line.</summary>
    private static SyntaxBlock FileOf(List<SyntaxLine> lines)
    {
        if (lines[^1].IsEmpty)
            lines.RemoveAt(lines.Count - 1);
        var file = new SyntaxBlock();
        file.Lines.AddRange(lines);
        return file;
    }

    /// <summary>
    /// The lines between the braces of a block: the spaces after <c>{</c> (with the line break, when nothing follows
    /// on its line) and before <c>}</c> become the block's own settings, so only whole lines stay lines.
    /// </summary>
    private static SyntaxBlock BlockOf(List<SyntaxLine> lines)
    {
        var block = new SyntaxBlock();
        var first = lines[0];
        if (lines.Count == 1 && first.Items.Count == 0)
        {
            block.BeforeClosing = first.Indentation + first.Trailing;
            return block;
        }

        if (first.Items.Count == 0)
        {
            block.AfterOpening = first.Indentation + first.Trailing + first.Ending;
            lines.RemoveAt(0);
        }
        else
        {
            block.AfterOpening = first.Indentation;
            first.Indentation = string.Empty;
        }

        var last = lines[^1];
        if (last.Items.Count == 0)
        {
            block.BeforeClosing = last.Indentation + last.Trailing;
            lines.RemoveAt(lines.Count - 1);
        }
        else
        {
            block.BeforeClosing = last.Trailing;
            last.Trailing = string.Empty;
        }
        block.Lines.AddRange(lines);
        return block;
    }

    private SyntaxError Unexpected() => new(_text.Position, $"unexpected \"{_text.Current}\"");

    private static bool IsSpace(char c) => c is ' ' or '\t' or '\r' or '\n';

    /// <summary>A syntax error at a position; it stops parsing, as nginx stops at the first error of a file.</summary>
    private sealed class SyntaxError(int position, string message) : Exception(message)
    {
        public int Position { get; } = position;
    }
}
