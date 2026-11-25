namespace Lakasabasz.Nginx.Sdk.Internal.Structure;

/// <summary>
/// The directives of a file or of a block in the internal structure, in order, with the style chosen for the block
/// (and, for a file, the comment at its top). It keeps track of what changed, and reports it with
/// <see cref="TakeChanges"/>. It holds no text: the style is a choice, the syntax tree writes the text by it.
/// </summary>
internal sealed class StructureBlock
{
    private readonly List<StructureNode> _nodes;
    private readonly HashSet<StructureNode> _added = [];
    private readonly List<StructureDirective> _removed = [];
    private readonly bool _readOnly;

    /// <summary>A block of these directives and comments; a directive that was in another block is now in this one.</summary>
    public StructureBlock(List<StructureNode> nodes, bool readOnly = false)
    {
        _nodes = nodes;
        _readOnly = readOnly;
        foreach (var directive in nodes.OfType<StructureDirective>())
            directive.Parent = this;
    }

    /// <summary>The block of a simple directive (or of a raw one): empty and not changeable.</summary>
    public static StructureBlock OfSimpleDirective() => new([], readOnly: true);

    public IReadOnlyList<StructureDirective> Directives => _nodes.OfType<StructureDirective>().ToList();

    /// <summary>The directives and comments, in order.</summary>
    public IReadOnlyList<StructureNode> Nodes => _nodes.AsReadOnly();

    /// <summary>The block directive this is the block of; <see langword="null"/> for a file.</summary>
    public StructureDirective? Owner { get; set; }

    /// <summary>
    /// How the block and its content are written, as stored; its line break counts only for a file (see
    /// <see cref="EffectiveStyle"/>).
    /// </summary>
    public TextStyle Style { get; private set; } = TextStyle.Default;

    /// <summary>Where <see cref="Style"/> comes from, which decides what it takes over when the block is added somewhere.</summary>
    public StyleOrigin StyleOrigin { get; private set; } = StyleOrigin.Default;

    /// <summary>The style with the line break of the file the block is in (its own, when it is in no file).</summary>
    public TextStyle EffectiveStyle => Root == this ? Style : Style with { LineBreak = Root.Style.LineBreak };

    /// <summary>The block at the top: the file, or the outermost loose block.</summary>
    public StructureBlock Root => Owner?.Parent?.Root ?? this;

    /// <summary>Whether the style was set in code since the changes were last taken.</summary>
    public bool StyleChanged { get; private set; }

    /// <summary>For a file: the comment at its top, without <c>#</c>, lines separated by <c>\n</c>; <see langword="null"/> for none.</summary>
    public string? Comment { get; private set; }

    public bool CommentChanged { get; private set; }

    public Result<StructureDirective, UsageError> Add(string name, IReadOnlyList<string> arguments) =>
        Attach(StructureDirective.NewSimple(name, arguments));

    public Result<StructureDirective, UsageError> AddBlock(string name, IReadOnlyList<string> arguments) =>
        Attach(StructureDirective.NewBlock(name, arguments, []));

    /// <summary>
    /// Adds a directive made elsewhere (a loose section) at the end; it may be in no other block. A block whose style
    /// was not set in code takes this block's style.
    /// </summary>
    public Result<StructureDirective, UsageError> Attach(StructureDirective directive)
    {
        if (NotABlockError() is { } error)
            return Result<StructureDirective, UsageError>.Failure(error);
        if (directive.Parent is not null)
            return Result<StructureDirective, UsageError>.Failure(new UsageError(UsageErrorKind.InvalidOperation,
                $"This {directive.Name} is in a file already: remove it there first, or add a new one."));
        _nodes.Add(directive);
        _added.Add(directive);
        directive.Parent = this;
        if (directive.Kind == DirectiveKind.Block)
            directive.Block.TakeStyleOf(EffectiveStyle);
        return Result<StructureDirective, UsageError>.Success(directive);
    }

    /// <summary>The first directive called <paramref name="name"/> gets the arguments, the others are removed; without any, it is added.</summary>
    public Result<StructureDirective, UsageError> Set(string name, IReadOnlyList<string> arguments)
    {
        if (NotABlockError() is { } error)
            return Result<StructureDirective, UsageError>.Failure(error);
        var existing = Directives.Where(directive => directive.Name == name).ToList();
        if (existing.Count == 0)
            return Add(name, arguments);

        existing[0].SetArguments(arguments);
        foreach (var duplicate in existing.Skip(1))
            Detach(duplicate);
        return Result<StructureDirective, UsageError>.Success(existing[0]);
    }

    /// <returns>How many directives called <paramref name="name"/> were removed.</returns>
    public Result<int, UsageError> Remove(string name)
    {
        if (NotABlockError() is { } error)
            return Result<int, UsageError>.Failure(error);
        var named = Directives.Where(directive => directive.Name == name).ToList();
        foreach (var directive in named)
            Detach(directive);
        return Result<int, UsageError>.Success(named.Count);
    }

    /// <returns>Whether the directive was in this block.</returns>
    public Result<bool, UsageError> Remove(StructureDirective directive) => NotABlockError() is { } error
        ? Result<bool, UsageError>.Failure(error)
        : Result<bool, UsageError>.Success(Detach(directive));

    /// <summary>The style set in code; in a file, its line break must be the file's.</summary>
    public Result<StructureBlock, UsageError> SetStyle(TextStyle style)
    {
        if (style.ValidationError() is { } problem)
            return Result<StructureBlock, UsageError>.Failure(problem);
        if (Root != this && style.LineBreak != Root.Style.LineBreak)
            return Result<StructureBlock, UsageError>.Failure(new UsageError(UsageErrorKind.Argument,
                "The line break is the file's: set it in the style of the file.", "value"));
        Style = style;
        StyleOrigin = StyleOrigin.Set;
        StyleChanged = true;
        return Result<StructureBlock, UsageError>.Success(this);
    }

    /// <summary>The style as read from a file or a piece of nginx text: not a change.</summary>
    public void ReadStyle(TextStyle style)
    {
        Style = style;
        StyleOrigin = StyleOrigin.Read;
    }

    /// <summary>The braces the block is written with now, e.g. after the tree put it onto lines to hold a block: not a change.</summary>
    public void NoteWrittenBraces(Braces braces) => Style = Style with { Braces = braces };

    /// <summary>For a file: the comment at its top, set in code.</summary>
    public Result<StructureBlock, UsageError> SetComment(string? comment)
    {
        if (StructureComment.CarriageReturnError(comment) is { } error)
            return Result<StructureBlock, UsageError>.Failure(error);
        Comment = comment;
        CommentChanged = true;
        return Result<StructureBlock, UsageError>.Success(this);
    }

    /// <summary>For a file: the comment at its top as read: not a change.</summary>
    public void ReadComment(string? comment) => Comment = comment;

    /// <summary>
    /// What changed in this block, and in the blocks in it, since the changes were last taken; from now on these
    /// count as unchanged. A directive added since is reported with everything in it, not as nested changes.
    /// </summary>
    public BlockChanges TakeChanges()
    {
        var changes = new BlockChanges { OwnCommentChanged = CommentChanged };
        CommentChanged = false;
        StyleChanged = false;
        changes.Removed.AddRange(_removed);
        foreach (var node in _nodes)
        {
            if (_added.Contains(node))
            {
                changes.Added.Add(node);
                if (node is StructureDirective added)
                    AcceptWithEverythingIn(added);
                continue;
            }

            if (node is not StructureDirective directive)
                continue;
            if (directive.ArgumentsChanged)
                changes.ArgumentsChanged.Add(directive);
            if (directive.CommentChanged)
                changes.CommentChanged.Add(directive);
            if (directive.Block.StyleChanged)
                changes.StyleChanged.Add(directive);
            directive.AcceptChanges();
            if (directive.Kind == DirectiveKind.Block)
                changes.Nested.Add((directive, directive.Block.TakeChanges()));
        }

        _added.Clear();
        _removed.Clear();
        return changes;
    }

    /// <summary>
    /// Takes over the style of the block this one is added to, unless it was set in code; one read from nginx text
    /// keeps its braces. The blocks inside follow, each from its own parent.
    /// </summary>
    private void TakeStyleOf(TextStyle parent)
    {
        if (StyleOrigin == StyleOrigin.Default)
            Style = parent;
        else if (StyleOrigin == StyleOrigin.Read)
            Style = parent with { Braces = Style.Braces };
        else
            Style = Style with { LineBreak = parent.LineBreak };
        foreach (var child in Directives.Where(directive => directive.Kind == DirectiveKind.Block))
            child.Block.TakeStyleOf(Style);
    }

    /// <summary>An added directive and everything in it count as written: their changes are forgotten.</summary>
    private static void AcceptWithEverythingIn(StructureDirective directive)
    {
        directive.AcceptChanges();
        directive.Block.TakeChanges();
    }

    private bool Detach(StructureDirective directive)
    {
        if (!_nodes.Remove(directive))
            return false;
        directive.Parent = null;
        if (!_added.Remove(directive))
            _removed.Add(directive);
        return true;
    }

    private UsageError? NotABlockError() => _readOnly
        ? new UsageError(UsageErrorKind.InvalidOperation, "Only a block directive contains directives.")
        : null;
}

/// <summary>Where the style of a block comes from.</summary>
internal enum StyleOrigin
{
    /// <summary>Not set: a new block takes the style of the block or file it is added to.</summary>
    Default,

    /// <summary>Read from a file or nginx text: the braces are the block's own, the rest follows where it is added.</summary>
    Read,

    /// <summary>Set in code: kept as it is.</summary>
    Set,
}
