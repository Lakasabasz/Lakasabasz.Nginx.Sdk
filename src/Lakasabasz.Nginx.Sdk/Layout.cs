namespace Lakasabasz.Nginx.Sdk;

/// <summary>
/// How a block is written. Used for new blocks and when it is set on a block; a block read from a file keeps its own
/// until then.
/// </summary>
public sealed record BlockLayout
{
    /// <summary>The usual layout: <c>{</c> at the end of the line, each directive on a line of its own.</summary>
    public static BlockLayout Default { get; } = new();

    /// <summary>
    /// The whole block on one line, e.g. <c>location /health { return 204; }</c>. A block holding a block or a comment
    /// is written on lines: adding one to a block on one line puts it onto lines, and its layout says so.
    /// </summary>
    public bool OneLine { get; init; }

    /// <summary>The <c>{</c> on a line of its own, under the block's name.</summary>
    public bool BraceOnNewLine { get; init; }
}

/// <summary>
/// How the new and changed text of a file is written: its line break and indentation unit. Read from the file when it
/// is loaded (the more common one wins); a new file gets <see cref="Default"/>. Text that is not changed keeps its
/// own, byte for byte.
/// </summary>
public sealed record NginxLayout
{
    /// <summary>Line breaks <c>\n</c>, indentation four spaces.</summary>
    public static NginxLayout Default { get; } = new();

    /// <summary>The line break: <c>\n</c> or <c>\r\n</c>.</summary>
    public string Newline { get; init; } = "\n";

    /// <summary>One level of indentation: spaces or tabs, e.g. <c>"\t"</c> or four spaces.</summary>
    public string Indentation { get; init; } = "    ";
}
