namespace Lakasabasz.Nginx.Sdk.Internal.Structure;

/// <summary>
/// The directives of a file or of a block in the internal structure, in order, with the comments added in code. It
/// keeps track of what changed, and reports it with <see cref="TakeChanges"/>: it knows nothing about how the
/// directives are written.
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

    /// <summary>The block of a simple directive: empty and not changeable.</summary>
    public static StructureBlock NotABlock() => new([], readOnly: true);

    public IReadOnlyList<StructureDirective> Directives => _nodes.OfType<StructureDirective>().ToList();

    /// <summary>The directives and comments, in order.</summary>
    public IReadOnlyList<StructureNode> Nodes => _nodes;

    public Result<StructureDirective, UsageError> Add(string name, IReadOnlyList<string> arguments) =>
        Attach(StructureDirective.NewSimple(name, arguments));

    public Result<StructureDirective, UsageError> AddBlock(string name, IReadOnlyList<string> arguments) =>
        Attach(StructureDirective.NewBlock(name, arguments, []));

    /// <summary>Adds a directive made elsewhere (a loose section) at the end; it may be in no other block.</summary>
    public Result<StructureDirective, UsageError> Attach(StructureDirective directive)
    {
        if (ReadOnlyError() is { } error)
            return Result<StructureDirective, UsageError>.Failure(error);
        if (directive.Parent is not null)
            return Result<StructureDirective, UsageError>.Failure(new UsageError(UsageErrorKind.InvalidOperation,
                $"This {directive.Name} is in a file already: remove it there first, or add a new one."));
        _nodes.Add(directive);
        _added.Add(directive);
        directive.Parent = this;
        return Result<StructureDirective, UsageError>.Success(directive);
    }

    /// <summary>Adds a comment on a line of its own after the last directive.</summary>
    public Result<StructureComment, UsageError> AddComment(string text)
    {
        if ((ReadOnlyError() ?? StructureComment.Check(text)) is { } error)
            return Result<StructureComment, UsageError>.Failure(error);
        var comment = new StructureComment(text);
        _nodes.Add(comment);
        _added.Add(comment);
        return Result<StructureComment, UsageError>.Success(comment);
    }

    /// <summary>The first directive called <paramref name="name"/> gets the arguments, the others are removed; without any, it is added.</summary>
    public Result<StructureDirective, UsageError> Set(string name, IReadOnlyList<string> arguments)
    {
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
        if (ReadOnlyError() is { } error)
            return Result<int, UsageError>.Failure(error);
        return Result<int, UsageError>.Success(Directives.Where(directive => directive.Name == name).ToList().Count(Detach));
    }

    /// <returns>Whether the directive was in this block.</returns>
    public Result<bool, UsageError> Remove(StructureDirective directive) => ReadOnlyError() is { } error
        ? Result<bool, UsageError>.Failure(error)
        : Result<bool, UsageError>.Success(Detach(directive));

    /// <summary>
    /// What changed in this block, and in the blocks in it, since the changes were last taken; from now on these
    /// count as unchanged. A directive added since is reported with everything in it, not as nested changes.
    /// </summary>
    public BlockChanges TakeChanges()
    {
        var changes = new BlockChanges();
        changes.Removed.AddRange(_removed);
        foreach (var node in _nodes)
        {
            if (_added.Contains(node))
            {
                changes.Added.Add(node);
                if (node is StructureDirective added)
                    AcceptAll(added);
                continue;
            }

            if (node is not StructureDirective directive)
                continue;
            if (directive.ArgumentsChanged)
                changes.Changed.Add(directive);
            if (directive.CommentChanged)
                changes.Commented.Add(directive);
            if (directive.StyleChanged)
                changes.Restyled.Add(directive);
            directive.AcceptChanges();
            if (directive.Kind == DirectiveKind.Block)
                changes.Nested.Add((directive, directive.Block.TakeChanges()));
        }

        _added.Clear();
        _removed.Clear();
        return changes;
    }

    private static void AcceptAll(StructureDirective directive)
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

    private UsageError? ReadOnlyError() => _readOnly
        ? new UsageError(UsageErrorKind.InvalidOperation, "Only a block directive contains directives.")
        : null;
}
