namespace Lakasabasz.Nginx.Sdk.Internal.Structure;

/// <summary>What a block of the internal structure holds: directives, and comments on lines of their own.</summary>
internal abstract class StructureNode;

/// <summary>
/// A comment on a line of its own that belongs to no directive, without <c>#</c>: read from a file or nginx text and
/// kept as it is; comments are added in code only to a directive, a block or a file.
/// </summary>
internal sealed class StructureComment(string text) : StructureNode
{
    public string Text { get; } = text;

    /// <summary>The error for a one-line comment (of a simple directive) with a line break; <see langword="null"/> for one without, or for none.</summary>
    public static UsageError? LineBreakError(string? text) => text is not null && text.IndexOfAny(['\r', '\n']) >= 0
        ? new UsageError(UsageErrorKind.Argument, "The comment of a directive is one line: it cannot contain a line break.", "value")
        : null;

    /// <summary>The error for a comment of several lines (of a block or a file) with <c>\r</c>: its lines are separated by <c>\n</c> only.</summary>
    public static UsageError? CarriageReturnError(string? text) => text is not null && text.IndexOf('\r') >= 0
        ? new UsageError(UsageErrorKind.Argument, "The lines of a comment are separated by \\n only.", "value")
        : null;
}
