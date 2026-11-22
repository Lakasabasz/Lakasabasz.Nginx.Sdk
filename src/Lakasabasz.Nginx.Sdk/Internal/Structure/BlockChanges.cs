namespace Lakasabasz.Nginx.Sdk.Internal.Structure;

/// <summary>
/// What changed in one block of the internal structure, in its own terms: the structure reports it
/// (<see cref="StructureBlock.TakeChanges"/>), the syntax tree turns it into edits of the text.
/// </summary>
internal sealed class BlockChanges
{
    /// <summary>Directives removed from the block.</summary>
    public List<StructureDirective> Removed { get; } = [];

    /// <summary>Directives whose arguments changed.</summary>
    public List<StructureDirective> Changed { get; } = [];

    /// <summary>Directives added, in order, with everything in them: they go after the last directive of the block.</summary>
    public List<StructureDirective> Added { get; } = [];

    /// <summary>The changes inside the blocks that were there before.</summary>
    public List<(StructureDirective Directive, BlockChanges Changes)> Nested { get; } = [];
}
