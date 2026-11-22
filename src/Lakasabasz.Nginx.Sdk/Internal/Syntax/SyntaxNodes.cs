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
