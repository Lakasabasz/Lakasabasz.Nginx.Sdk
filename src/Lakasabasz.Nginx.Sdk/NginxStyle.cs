namespace Lakasabasz.Nginx.Sdk;

/// <summary>
/// How new and changed text is written: of a whole file (<see cref="NginxFile.Style"/>) or of one block
/// (<see cref="Section.Style"/>). Text that is not changed keeps its own, byte for byte.
/// </summary>
/// <remarks>
/// <para>A file's style is read from it (in each field, what most of the file does) or, for a new file,
/// <see cref="Default"/>. Its <see cref="Braces"/> is the default for new blocks; its <see cref="Indentation"/> and
/// <see cref="BlockSpacing"/> apply to the top level.</para>
/// <para>A block's style describes the block itself (<see cref="Braces"/>) and its content (<see cref="Indentation"/>,
/// <see cref="BlockSpacing"/>). A block read from a file has its own braces and the file's other values; a new block
/// takes the style of the block or file it is added to, unless one was set on it. The line break is always the
/// file's.</para>
/// </remarks>
public sealed record NginxStyle
{
    /// <summary>Line breaks <c>\n</c>, four spaces, <c>{</c> at the end of the line, one empty line before a block.</summary>
    public static NginxStyle Default { get; } = new();

    /// <summary>The line break.</summary>
    public LineBreak LineBreak { get; init; } = LineBreak.Lf;

    /// <summary>One level of indentation, e.g. <c>Indentation.Spaces(4)</c> or <c>Indentation.Tabs()</c>.</summary>
    public Indentation Indentation { get; init; } = Indentation.Spaces(4);

    /// <summary>How a block is written: <c>{</c> at the end of its line, on a line of its own, or the whole block on one line.</summary>
    public BraceStyle Braces { get; init; } = BraceStyle.EndOfLine;

    /// <summary>The empty lines before a block that follows another directive (none before the first one of its block or file).</summary>
    public int BlockSpacing { get; init; } = 1;
}
