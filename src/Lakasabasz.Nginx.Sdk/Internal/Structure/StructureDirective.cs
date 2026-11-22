namespace Lakasabasz.Nginx.Sdk.Internal.Structure;

/// <summary>A directive of the internal structure: its name, arguments and block, without any formatting.</summary>
internal sealed class StructureDirective
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

    public string? RawContent { get; }

    /// <summary>Where the directive starts in the file as read; 0 for a directive added in code.</summary>
    public int Line { get; }

    public int Column { get; }

    /// <summary>Whether the arguments changed since the changes were last taken.</summary>
    public bool ArgumentsChanged { get; private set; }

    public void SetArguments(IReadOnlyList<string> arguments)
    {
        Arguments = arguments;
        ArgumentsChanged = true;
    }

    /// <summary>Forgets that the arguments changed: the change was taken.</summary>
    public void AcceptArguments() => ArgumentsChanged = false;
}

internal enum DirectiveKind
{
    Simple,
    Block,
    Raw,
}
