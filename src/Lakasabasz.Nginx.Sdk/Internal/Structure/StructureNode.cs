namespace Lakasabasz.Nginx.Sdk.Internal.Structure;

/// <summary>What a block of the internal structure holds: directives, and comments on lines of their own.</summary>
internal abstract class StructureNode;

/// <summary>A comment on a line of its own, without <c>#</c>.</summary>
internal sealed class StructureComment(string text) : StructureNode
{
    public string Text { get; } = text;

    /// <summary>The error for a comment with a line break (a comment is one line); <see langword="null"/> for one without, or for none.</summary>
    public static UsageError? LineBreakError(string? text) => text is not null && text.IndexOfAny(['\r', '\n']) >= 0
        ? new UsageError(UsageErrorKind.Argument, "A comment is one line: it cannot contain a line break.", "comment")
        : null;
}
