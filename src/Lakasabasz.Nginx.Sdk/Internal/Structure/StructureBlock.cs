namespace Lakasabasz.Nginx.Sdk.Internal.Structure;

/// <summary>
/// The directives of a file or of a block in the internal structure, in order. It keeps track of what changed, and
/// reports it with <see cref="TakeChanges"/>: it knows nothing about how the directives are written.
/// </summary>
internal sealed class StructureBlock
{
    private readonly List<StructureDirective> _directives;
    private readonly HashSet<StructureDirective> _added = [];
    private readonly List<StructureDirective> _removed = [];
    private readonly bool _readOnly;

    public StructureBlock(List<StructureDirective> directives, bool readOnly = false)
    {
        _directives = directives;
        _readOnly = readOnly;
    }

    /// <summary>The block of a simple directive: empty and not changeable.</summary>
    public static StructureBlock NotABlock() => new([], readOnly: true);

    public IReadOnlyList<StructureDirective> Directives => _directives;

    public Result<StructureDirective, UsageError> Add(string name, IReadOnlyList<string> arguments) =>
        Append(new StructureDirective(name, arguments, DirectiveKind.Simple, null, null));

    public Result<StructureDirective, UsageError> AddBlock(string name, IReadOnlyList<string> arguments) =>
        Append(new StructureDirective(name, arguments, DirectiveKind.Block, new StructureBlock([]), null));

    /// <summary>The first directive called <paramref name="name"/> gets the arguments, the others are removed; without any, it is added.</summary>
    public Result<StructureDirective, UsageError> Set(string name, IReadOnlyList<string> arguments)
    {
        var existing = _directives.Where(directive => directive.Name == name).ToList();
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
        return Result<int, UsageError>.Success(_directives.Where(directive => directive.Name == name).ToList().Count(Detach));
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
        foreach (var directive in _directives)
        {
            if (_added.Contains(directive))
            {
                changes.Added.Add(directive);
                directive.AcceptArguments();
                directive.Block.TakeChanges();
                continue;
            }

            if (directive.ArgumentsChanged)
                changes.Changed.Add(directive);
            directive.AcceptArguments();
            if (directive.Kind == DirectiveKind.Block)
                changes.Nested.Add((directive, directive.Block.TakeChanges()));
        }

        _added.Clear();
        _removed.Clear();
        return changes;
    }

    private Result<StructureDirective, UsageError> Append(StructureDirective directive)
    {
        if (ReadOnlyError() is { } error)
            return Result<StructureDirective, UsageError>.Failure(error);
        _directives.Add(directive);
        _added.Add(directive);
        return Result<StructureDirective, UsageError>.Success(directive);
    }

    private bool Detach(StructureDirective directive)
    {
        if (!_directives.Remove(directive))
            return false;
        if (!_added.Remove(directive))
            _removed.Add(directive);
        return true;
    }

    private UsageError? ReadOnlyError() => _readOnly
        ? new UsageError(UsageErrorKind.InvalidOperation, "Only a block directive contains directives.")
        : null;
}
