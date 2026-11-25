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
    public List<StructureDirective> ArgumentsChanged { get; } = [];

    /// <summary>Directives whose comment changed.</summary>
    public List<StructureDirective> CommentChanged { get; } = [];

    /// <summary>For a file: whether the comment at its top changed.</summary>
    public bool OwnCommentChanged { get; init; }

    /// <summary>Block directives whose style was set.</summary>
    public List<StructureDirective> StyleChanged { get; } = [];

    /// <summary>Directives and comments added, in order, with everything in them: they go after the last directive of the block.</summary>
    public List<StructureNode> Added { get; } = [];

    /// <summary>
    /// The changes inside the blocks that were there before (an added block is in <see cref="Added"/> with everything
    /// in it). A directive may be in several lists, e.g. with changed arguments and a changed comment.
    /// </summary>
    public List<(StructureDirective Directive, BlockChanges Changes)> Nested { get; } = [];
}
