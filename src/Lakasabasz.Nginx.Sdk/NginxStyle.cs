using Lakasabasz.Nginx.Sdk.Internal;

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
    private const string Lf = "\n";
    private const string Crlf = "\r\n";

    /// <summary>Line breaks <c>\n</c>, four spaces, <c>{</c> at the end of the line, one empty line before a block.</summary>
    public static NginxStyle Default { get; } = new();

    /// <summary>The line break: <c>\n</c> or <c>\r\n</c>.</summary>
    public string LineBreak { get; init; } = Lf;

    /// <summary>One level of indentation: spaces or tabs, e.g. <c>"\t"</c> or four spaces.</summary>
    public string Indentation { get; init; } = "    ";

    /// <summary>How a block is written: <c>{</c> at the end of its line, on a line of its own, or the whole block on one line.</summary>
    public BraceStyle Braces { get; init; } = BraceStyle.EndOfLine;

    /// <summary>The empty lines before a block that follows another directive (none before the first one of its block or file).</summary>
    public int BlockSpacing { get; init; } = 1;

    /// <summary>What makes the style unusable, e.g. a line break that is no line break; <see langword="null"/> for a usable one.</summary>
    internal UsageError? Problem()
    {
        if (LineBreak is not (Lf or Crlf))
            return new UsageError(UsageErrorKind.Argument, "The line break is \\n or \\r\\n.", "value");
        if (Indentation.Length == 0 || Indentation.Any(c => c is not (' ' or '\t')))
            return new UsageError(UsageErrorKind.Argument, "The indentation is spaces or tabs.", "value");
        if (!Enum.IsDefined(typeof(BraceStyle), Braces))
            return new UsageError(UsageErrorKind.Argument, $"{Braces} is not a brace style.", "value");
        if (BlockSpacing < 0)
            return new UsageError(UsageErrorKind.Argument, "The spacing before a block is 0 or more empty lines.", "value");
        return null;
    }
}

/// <summary>How a block is written.</summary>
public enum BraceStyle
{
    /// <summary><c>{</c> at the end of the block's line, each directive on a line of its own: <c>server {</c>.</summary>
    EndOfLine,

    /// <summary><c>{</c> on a line of its own, under the block's name.</summary>
    NewLine,

    /// <summary>The whole block on one line: <c>location /health { return 204; }</c>. A block holding a block or a comment cannot be.</summary>
    OneLine,
}
