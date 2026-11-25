using System.Text;

namespace Lakasabasz.Nginx.Sdk.Internal.Syntax;

/// <summary>
/// The lines of a file or of a block, with the spacing around the braces. A file is a block without braces. Written
/// back exactly as read: every character of the file is in one of the fields below.
/// </summary>
internal sealed partial class SyntaxBlock
{
    /// <summary>The lines; in a block, the first one starts on the line of <c>{</c> when <see cref="AfterOpening"/> has no line break.</summary>
    public List<SyntaxLine> Lines { get; } = [];

    /// <summary>The spaces after <c>{</c> and, when the lines start below it, the line break.</summary>
    public string AfterOpening { get; set; } = string.Empty;

    /// <summary>The spaces before <c>}</c>: its indentation when it has a line of its own.</summary>
    public string BeforeClosing { get; set; } = string.Empty;

    /// <summary>The block of a file: its lines, no braces.</summary>
    public static SyntaxBlock PrepareFile(IEnumerable<SyntaxLine> lines)
    {
        var file = new SyntaxBlock();
        file.Lines.AddRange(lines);
        return file;
    }

    /// <summary>A block in braces: what stands right after <c>{</c>, its whole lines, what stands right before <c>}</c>.</summary>
    public static SyntaxBlock PrepareBetweenBraces(string afterOpening, IEnumerable<SyntaxLine> lines, string beforeClosing)
    {
        var block = new SyntaxBlock { AfterOpening = afterOpening, BeforeClosing = beforeClosing };
        block.Lines.AddRange(lines);
        return block;
    }

    /// <summary>Whether the whole block is on one line, e.g. <c>events { worker_connections 64; }</c>.</summary>
    public bool IsOneLine => AfterOpening.IndexOf('\n') < 0 && Lines.All(line => line.Ending.Length == 0);

    public void Render(StringBuilder output)
    {
        foreach (var line in Lines)
            line.Render(output);
    }

    public void RenderBody(StringBuilder output)
    {
        output.Append('{').Append(AfterOpening);
        Render(output);
        output.Append(BeforeClosing).Append('}');
    }
}

/// <summary>One line: its indentation, the directives and comments on it, and how it ends.</summary>
internal sealed class SyntaxLine
{
    public string Indentation { get; set; } = string.Empty;

    public List<SyntaxItem> Items { get; } = [];

    /// <summary>The spaces after the last item.</summary>
    public string Trailing { get; set; } = string.Empty;

    /// <summary><c>\n</c> or <c>\r\n</c>; empty for the last line of a file without a final line break and before <c>}</c>.</summary>
    public string Ending { get; set; } = string.Empty;

    public bool IsEmpty => Indentation.Length == 0 && Items.Count == 0 && Trailing.Length == 0 && Ending.Length == 0;

    public bool HasDirectives => Items.Any(item => item.Node is SyntaxDirective);

    /// <summary>A line being prepared: only its indentation so far, its items and end still to come.</summary>
    public static SyntaxLine PrepareWithIndent(string indentation) => new() { Indentation = indentation };

    /// <summary>An empty line: only its line break.</summary>
    public static SyntaxLine Blank(string lineBreak) => new() { Ending = lineBreak };

    /// <summary>A line holding only <paramref name="node"/>, indented and ended as given.</summary>
    public static SyntaxLine Holding(SyntaxNode node, string indentation, string lineBreak)
    {
        var line = new SyntaxLine { Indentation = indentation, Ending = lineBreak };
        line.Items.Add(new SyntaxItem(string.Empty, node));
        return line;
    }

    /// <summary>Ends the line: the whitespace after its last item, and its line break (empty for none).</summary>
    public SyntaxLine EndWith(string trailing, string ending)
    {
        Trailing = trailing;
        Ending = ending;
        return this;
    }

    /// <summary>
    /// Whether the line ends with its line break here. It does not at the end of a file without a final line break,
    /// and in a block when the block's <c>}</c> stands on the same line of text: the block's part of that line ends
    /// before <c>}</c>; the <c>}</c> and the line break after it belong to the line of the block's directive.
    /// </summary>
    public bool EndsWithLineBreak => Ending.Length > 0;

    /// <summary>A line with nothing on it but whitespace.</summary>
    public bool IsBlank => Items.Count == 0;

    /// <summary>A line holding only a comment.</summary>
    public bool IsCommentLine => Items is [{ Node: SyntaxComment }];

    public void Render(StringBuilder output)
    {
        output.Append(Indentation);
        foreach (var item in Items)
            item.Render(output);
        output.Append(Trailing).Append(Ending);
    }
}

/// <summary>A directive or a comment on a line, with the spaces before it.</summary>
internal sealed class SyntaxItem(string spacing, SyntaxNode node)
{
    public string Spacing { get; set; } = spacing;

    public SyntaxNode Node { get; } = node;

    public void Render(StringBuilder output)
    {
        output.Append(Spacing);
        Node.Render(output);
    }
}

internal abstract class SyntaxNode
{
    public abstract void Render(StringBuilder output);
}

/// <summary>A comment, up to the end of its line.</summary>
internal sealed class SyntaxComment(string text) : SyntaxNode
{
    /// <summary>The comment with its <c>#</c>.</summary>
    public string Text { get; } = text;

    public override void Render(StringBuilder output) => output.Append(Text);
}

/// <summary>A directive as written: its tokens, its header text, and its block or raw body.</summary>
internal sealed class SyntaxDirective(string name, IReadOnlyList<string> arguments, string header, SyntaxBlock? block, string? raw,
    int line = 0, int column = 0) : SyntaxNode
{
    public string Name { get; } = name;

    /// <summary>The arguments as nginx reads them (unquoted, escapes resolved).</summary>
    public IReadOnlyList<string> Arguments { get; set; } = arguments;

    /// <summary>The name and arguments exactly as written, up to <c>;</c> or <c>{</c>.</summary>
    public string Header { get; set; } = header;

    /// <summary>The block's lines; <see langword="null"/> for a simple directive and a raw block.</summary>
    public SyntaxBlock? Block { get; } = block;

    /// <summary>The body of a raw block (e.g. Lua code), between the braces.</summary>
    public string? Raw { get; } = raw;

    /// <summary>Where the directive starts in the file as read; 0 for a directive added later.</summary>
    public int Line { get; } = line;

    public int Column { get; } = column;

    /// <summary>Whether the directive has a body in braces, parsed or raw.</summary>
    public bool HasBody => Block is not null || Raw is not null;

    /// <summary>The whitespace at the end of the header: what stands between a block's arguments and its <c>{</c>.</summary>
    public string BraceSpacing => Header.Substring(Header.TrimEnd().Length);

    /// <summary>Whether a block's <c>{</c> is on a line of its own: a line break stands before it.</summary>
    public bool BraceOnNewLine => BraceSpacing.IndexOf('\n') >= 0;

    public override void Render(StringBuilder output)
    {
        output.Append(Header);
        if (Block is not null)
            Block.RenderBody(output);
        else if (Raw is not null)
            output.Append('{').Append(Raw).Append('}');
        else
            output.Append(';');
    }
}
