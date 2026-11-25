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
/// Every step works on the cursor it is given: <c>Take…</c> takes text from it as written, <c>Parse…</c> takes text and
/// turns it into words or nodes, <c>Skip…</c> moves past text; the cursor's <c>Read…</c> and the questions <c>At…</c> / <c>IsAt…</c> only look. Parsing stops
/// at the first error of a file, as nginx does: each step returns its result or the error, and a step that gets an
/// error returns it at once. This file reads lines and directives; words are in
/// ConfigParser.Words.cs, raw Lua blocks in ConfigParser.Lua.cs.
/// </remarks>
internal static partial class ConfigParser
{
    private const string EndOfFileInBlock = "unexpected end of file, expecting \"}\"";
    private const string EndOfFileInDirective = "unexpected end of file, expecting \";\" or \"}\"";

    /// <summary>The syntax tree of the file, or its syntax error.</summary>
    public static Result<SyntaxTree, NginxParseError> Parse(NginxPath file, string text)
    {
        var cursor = new TextCursor(text);
        return ParseFile(cursor)
            .Map(root => new SyntaxTree(root, text))
            .MapError(problem => ErrorOf(file, problem));
    }

    private static Result<SyntaxBlock, SyntaxProblem> ParseFile(TextCursor cursor) => ParseLines(cursor, insideBlock: false).Map(FileOf);

    /// <summary>The lines of a file, or of a block up to its closing <c>}</c> (consumed).</summary>
    private static Result<List<SyntaxLine>, SyntaxProblem> ParseLines(TextCursor cursor, bool insideBlock)
    {
        var lines = new List<SyntaxLine>();
        while (lines.Count == 0 || lines[^1].EndsWithLineBreak)
        {
            var line = ParseLine(cursor, insideBlock);
            if (!line.IsSuccess)
                return Failure<List<SyntaxLine>>(line.Error);
            lines.Add(line.Value);
        }

        if (!insideBlock)
            return Success(lines);
        if (cursor.AtEnd)
            return Failure<List<SyntaxLine>>(Problem(cursor, EndOfFileInBlock));
        cursor.Advance();
        return Success(lines);
    }

    /// <summary>
    /// One line: its comments and directives up to its line break (consumed), the end of the text, or, in a block, the
    /// block's <c>}</c> (not consumed).
    /// </summary>
    private static Result<SyntaxLine, SyntaxProblem> ParseLine(TextCursor cursor, bool insideBlock)
    {
        var line = SyntaxLine.PrepareWithIndent(TakeBlanks(cursor));
        var spacing = string.Empty;
        while (!IsAtLineEnd(cursor, insideBlock))
        {
            var item = ParseItem(cursor);
            if (!item.IsSuccess)
                return Failure<SyntaxLine>(item.Error);
            line.Items.Add(new SyntaxItem(spacing, item.Value));
            spacing = TakeBlanks(cursor);
        }
        return Success(line.EndWith(spacing, TakeLineBreak(cursor)));
    }

    /// <summary>Whether the cursor is at the end of a line (it does not move): the end of the text, a line break, or, in a block, its <c>}</c>.</summary>
    private static bool IsAtLineEnd(TextCursor cursor, bool insideBlock) =>
        cursor.AtEnd || cursor.AtLineBreak || (insideBlock && cursor.Current.IsBlockEnd());

    /// <summary>The next thing on a line: a comment or a directive; <c>}</c>, <c>;</c> or <c>{</c> cannot start one.</summary>
    private static Result<SyntaxNode, SyntaxProblem> ParseItem(TextCursor cursor) => cursor.Current switch
    {
        var c when c.IsBlockBegin() || c.IsBlockEnd() || c.IsDirectiveEnd() => Failure<SyntaxNode>(Unexpected(cursor)),
        var c when c.IsCommentStart() => Success<SyntaxNode>(TakeComment(cursor)),
        _ => ParseDirective(cursor).Map(SyntaxNode (directive) => directive),
    };

    private static Result<SyntaxDirective, SyntaxProblem> ParseDirective(TextCursor cursor)
    {
        var start = cursor.CurrentPosition;
        var words = ParseWordsOfDirective(cursor);
        if (!words.IsSuccess)
            return Failure<SyntaxDirective>(words.Error);

        var header = cursor.ReadSince(start);
        var endsWithBlock = cursor.Current.IsBlockBegin();
        cursor.Advance();

        var name = words.Value[0];
        var arguments = words.Value.Skip(1).ToArray();
        if (!endsWithBlock)
            return Success(new SyntaxDirective(name, arguments, header, null, null, start.Line, start.Column));
        return IsRawBlock(name)
            ? TakeRawBody(cursor).Map(body => new SyntaxDirective(name, arguments, header, null, body, start.Line, start.Column))
            : ParseLines(cursor, insideBlock: true).Map(lines => new SyntaxDirective(name, arguments, header, BlockOf(lines), null, start.Line, start.Column));
    }

    /// <summary>The name and arguments of a directive, up to its <c>;</c> or <c>{</c> (not consumed).</summary>
    private static Result<List<string>, SyntaxProblem> ParseWordsOfDirective(TextCursor cursor)
    {
        var words = new List<string>();
        while (!cursor.AtEnd && !IsAtEndOfWords(cursor))
        {
            if (cursor.Current.IsBlockEnd())
                return Failure<List<string>>(Unexpected(cursor));
            var word = ParseWord(cursor);
            if (!word.IsSuccess)
                return Failure<List<string>>(word.Error);
            words.Add(word.Value);
            SkipSpaceAndComments(cursor);
        }
        return cursor.AtEnd ? Failure<List<string>>(Problem(cursor, EndOfFileInDirective)) : Success(words);
    }

    /// <summary>Whether the cursor is at the end of a directive's words (it does not move): its <c>;</c> or <c>{</c>.</summary>
    private static bool IsAtEndOfWords(TextCursor cursor) => cursor.Current.IsDirectiveEnd() || cursor.Current.IsBlockBegin();

    /// <summary>Whitespace, line breaks and comments between the words of a directive.</summary>
    private static void SkipSpaceAndComments(TextCursor cursor)
    {
        while (!cursor.AtEnd)
        {
            if (cursor.Current.IsCommentStart())
                SkipToLineEnd(cursor);
            else if (cursor.Current.IsSpace())
                cursor.Advance();
            else
                return;
        }
    }

    /// <summary>Spaces and tabs (and a <c>\r</c> not starting a line break): the spacing on a line.</summary>
    private static string TakeBlanks(TextCursor cursor)
    {
        var start = cursor.CurrentPosition;
        while (!cursor.AtEnd && (cursor.Current is ' ' or '\t' || cursor.Current == '\r' && !cursor.AtLineBreak))
            cursor.Advance();
        return cursor.ReadSince(start);
    }

    /// <summary>The line break here, consumed; empty when there is none.</summary>
    private static string TakeLineBreak(TextCursor cursor)
    {
        var start = cursor.CurrentPosition;
        cursor.Advance(cursor.LineBreakLength);
        return cursor.ReadSince(start);
    }

    private static SyntaxComment TakeComment(TextCursor cursor)
    {
        var start = cursor.CurrentPosition;
        while (!cursor.AtEnd && !cursor.AtLineBreak)
            cursor.Advance();
        return new SyntaxComment(cursor.ReadSince(start));
    }

    /// <summary>Up to the <c>\n</c> ending the line (not consumed).</summary>
    private static void SkipToLineEnd(TextCursor cursor)
    {
        while (!cursor.AtEnd && cursor.Current != '\n')
            cursor.Advance();
    }

    /// <summary>The lines of a file; the empty rest after the last line break is not a line.</summary>
    private static SyntaxBlock FileOf(List<SyntaxLine> lines)
    {
        if (lines[^1].IsEmpty)
            lines.RemoveAt(lines.Count - 1);
        return SyntaxBlock.PrepareFile(lines);
    }

    /// <summary>
    /// The lines between the braces of a block: the spaces after <c>{</c> (with the line break, when nothing follows
    /// on its line) and before <c>}</c> become the block's own settings, so only whole lines stay lines.
    /// </summary>
    private static SyntaxBlock BlockOf(List<SyntaxLine> lines)
    {
        var afterOpening = TakeOpening(lines);
        var beforeClosing = TakeClosing(lines);
        return SyntaxBlock.PrepareBetweenBraces(afterOpening, lines, beforeClosing);
    }

    /// <summary>
    /// Takes what stands right after <c>{</c>: the spaces before the first item on its line, or, when nothing follows
    /// <c>{</c> on its line, that whole rest of the line with its line break. In an empty block it takes nothing: the
    /// spaces between the braces stand before <c>}</c>.
    /// </summary>
    private static string TakeOpening(List<SyntaxLine> lines)
    {
        var first = lines[0];
        if (!first.IsBlank)
        {
            var indentation = first.Indentation;
            first.Indentation = string.Empty;
            return indentation;
        }

        if (IsEmptyBlock(lines))
            return string.Empty;
        lines.RemoveAt(0);
        return first.Indentation + first.Trailing + first.Ending;
    }

    /// <summary>Whether there is nothing between the braces but spaces: <c>{ }</c>, <c>{}</c>.</summary>
    private static bool IsEmptyBlock(List<SyntaxLine> lines) => lines is [{ IsBlank: true }];

    /// <summary>
    /// Takes what stands right before <c>}</c>: the spaces after the last item on its line, or, when <c>}</c> starts its
    /// line, the indentation before it.
    /// </summary>
    private static string TakeClosing(List<SyntaxLine> lines)
    {
        var last = lines[^1];
        if (!last.IsBlank)
        {
            var trailing = last.Trailing;
            last.Trailing = string.Empty;
            return trailing;
        }

        lines.RemoveAt(lines.Count - 1);
        return last.Indentation + last.Trailing;
    }

    /// <summary>The parse error of <paramref name="file"/> for a problem.</summary>
    private static NginxParseError ErrorOf(NginxPath file, SyntaxProblem problem)
    {
        return new NginxParseError(file, problem.Position.Line, problem.Position.Column, problem.Message);
    }

    /// <summary>The error for the character at the current position.</summary>
    private static SyntaxProblem Unexpected(TextCursor cursor) => Problem(cursor, $"unexpected \"{cursor.Current}\"");

    /// <summary>An error at the current position.</summary>
    private static SyntaxProblem Problem(TextCursor cursor, string message) => new(cursor.CurrentPosition, message);

    private static Result<T, SyntaxProblem> Success<T>(T value) => Result<T, SyntaxProblem>.Success(value);

    private static Result<T, SyntaxProblem> Failure<T>(SyntaxProblem problem) => Result<T, SyntaxProblem>.Failure(problem);


    /// <summary>A syntax error at a position of the text, in nginx's words; it ends the parsing of the file.</summary>
    private sealed record SyntaxProblem(TextPosition Position, string Message);
}
