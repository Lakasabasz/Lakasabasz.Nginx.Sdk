namespace Lakasabasz.Nginx.Sdk.Internal.Structure;

/// <summary>An entry of a block in the internal structure: a directive, or a comment added in code.</summary>
internal abstract class StructureNode;

/// <summary>A comment added in code, on a line of its own; comments read from a file stay in the syntax tree only.</summary>
internal sealed class StructureComment(string text) : StructureNode
{
    /// <summary>The text after <c>#</c>.</summary>
    public string Text { get; } = text;
}
