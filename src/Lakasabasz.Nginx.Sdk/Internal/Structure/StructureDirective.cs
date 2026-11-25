namespace Lakasabasz.Nginx.Sdk.Internal.Structure;

/// <summary>
/// A directive of the internal structure: its name, arguments, block, the comment after it and the style its block
/// is written in. It holds no text: the style is a choice (one line, brace on a new line), the syntax tree writes it.
/// </summary>
internal sealed class StructureDirective : StructureNode
{
    public StructureDirective(string name, IReadOnlyList<string> arguments, DirectiveKind kind, StructureBlock? block,
        string? rawContent, int line = 0, int column = 0)
    {
        Name = name;
        Arguments = arguments;
        Kind = kind;
        Block = block ?? StructureBlock.OfSimpleDirective();
        RawContent = rawContent;
        Line = line;
        Column = column;
    }

    /// <summary>A new simple directive, e.g. <c>listen 80;</c>, made in code.</summary>
    public static StructureDirective NewSimple(string name, IReadOnlyList<string> arguments) =>
        new(name, arguments, DirectiveKind.Simple, block: null, rawContent: null);

    /// <summary>A new block directive made in code, holding <paramref name="body"/> (directives and comments).</summary>
    public static StructureDirective NewBlock(string name, IReadOnlyList<string> arguments, List<StructureNode> body) =>
        new(name, arguments, DirectiveKind.Block, new StructureBlock(body), rawContent: null);

    public string Name { get; }

    public IReadOnlyList<string> Arguments { get; private set; }

    public DirectiveKind Kind { get; }

    /// <summary>Whether the directive has a body in braces: a block, or a raw block.</summary>
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
    public string? SourceText { get; private set; }

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

    public Result<StructureDirective, UsageError> SetComment(string? comment)
    {
        if (StructureComment.LineBreakError(comment) is { } error)
            return Result<StructureDirective, UsageError>.Failure(error);
        Comment = comment;
        CommentChanged = true;
        return Result<StructureDirective, UsageError>.Success(this);
    }

    public void SetStyle(BlockStyle style)
    {
        Style = style;
        StyleChanged = true;
    }

    /// <summary>The comment, style and text of the directive as read from a file: not a change.</summary>
    public void Read(string? comment, BlockStyle style, string? sourceText)
    {
        Comment = comment;
        Style = style;
        SourceText = sourceText;
    }

    /// <summary>The style the block is written in now, e.g. after the tree put it onto lines to hold a block: not a change.</summary>
    public void NoteWrittenStyle(BlockStyle style) => Style = style;

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
    /// <summary>Ends with <c>;</c>, e.g. <c>listen 80;</c>.</summary>
    Simple,

    /// <summary>A block of directives, e.g. <c>server { … }</c>.</summary>
    Block,

    /// <summary>A block whose body is kept as text, not directives, e.g. <c>content_by_lua_block { … }</c>.</summary>
    Raw,
}
