namespace Lakasabasz.Nginx.Sdk.Internal.Structure;

/// <summary>
/// A directive of the internal structure: its name, arguments, block, the comment after it and the style its block
/// is written in. No formatting of its own: the syntax tree writes it.
/// </summary>
internal sealed class StructureDirective : StructureNode
{
    public StructureDirective(string name, IReadOnlyList<string> arguments, DirectiveKind kind, StructureBlock? block,
        string? rawContent, int line = 0, int column = 0)
    {
        Name = name;
        Arguments = arguments;
        Kind = kind;
        Block = block ?? StructureBlock.NotABlock();
        RawContent = rawContent;
        Line = line;
        Column = column;
    }

    public string Name { get; }

    public IReadOnlyList<string> Arguments { get; private set; }

    public DirectiveKind Kind { get; }

    public bool IsBlock => Kind != DirectiveKind.Simple;

    /// <summary>The directives inside the block; empty and not changeable for a simple directive or a raw block.</summary>
    public StructureBlock Block { get; }

    /// <summary>The block this directive is in; <see langword="null"/> for a directive not added anywhere (yet) or removed.</summary>
    public StructureBlock? Parent { get; set; }

    public string? RawContent { get; }

    /// <summary>Where the directive starts in the file as read; 0 for a directive added in code.</summary>
    public int Line { get; }

    public int Column { get; }

    /// <summary>The directive as written in the file when it was read; <see langword="null"/> for one added in code.</summary>
    public string? SourceText { get; set; }

    /// <summary>The comment after the directive on its line, without <c>#</c>; <see langword="null"/> for none.</summary>
    public string? Comment { get; private set; }

    /// <summary>How the block is written: on one line, with <c>{</c> on a line of its own.</summary>
    public BlockStyle Style { get; private set; } = BlockStyle.Default;

    /// <summary>Whether the arguments changed since the changes were last taken.</summary>
    public bool ArgumentsChanged { get; private set; }

    public bool CommentChanged { get; private set; }

    public bool StyleChanged { get; private set; }

    public void SetArguments(IReadOnlyList<string> arguments)
    {
        Arguments = arguments;
        ArgumentsChanged = true;
    }

    public void SetComment(string? comment)
    {
        Comment = comment;
        CommentChanged = true;
    }

    public void SetStyle(BlockStyle style)
    {
        Style = style;
        StyleChanged = true;
    }

    /// <summary>The comment and style as read from a file: not a change.</summary>
    public void Read(string? comment, BlockStyle style, string sourceText)
    {
        Comment = comment;
        Style = style;
        SourceText = sourceText;
    }

    /// <summary>Forgets what changed: the changes were taken.</summary>
    public void AcceptChanges()
    {
        ArgumentsChanged = false;
        CommentChanged = false;
        StyleChanged = false;
    }
}

/// <summary>How a block is written.</summary>
/// <param name="OneLine">The whole block on one line, e.g. <c>events { worker_connections 64; }</c>.</param>
/// <param name="BraceOnNewLine">The <c>{</c> on a line of its own, under the directive's name.</param>
internal sealed record BlockStyle(bool OneLine, bool BraceOnNewLine)
{
    public static BlockStyle Default { get; } = new(false, false);
}

internal enum DirectiveKind
{
    Simple,
    Block,
    Raw,
}
