namespace Lakasabasz.Nginx.Sdk.Internal;

/// <summary>
/// How new and changed text is written, of a file or of a block: the structure keeps it, the syntax tree writes by it.
/// The internal counterpart of the public style, so either can change without the other.
/// </summary>
/// <param name="LineBreak">The line break: <c>\n</c> or <c>\r\n</c>; a block's is always its file's.</param>
/// <param name="Indentation">One level of indentation, spaces or tabs.</param>
/// <param name="Braces">How a block is written; for a file, how its new blocks are.</param>
/// <param name="BlockSpacing">The empty lines before a block that follows another directive.</param>
internal sealed record TextStyle(string LineBreak, string Indentation, Braces Braces, int BlockSpacing)
{
    public const string Lf = "\n";
    public const string Crlf = "\r\n";

    /// <summary>Line breaks <c>\n</c>, four spaces, <c>{</c> at the end of the line, one empty line before a block.</summary>
    public static TextStyle Default { get; } = new(Lf, "    ", Braces.EndOfLine, 1);

    /// <summary>The error that makes the style unusable, e.g. a line break that is no line break; <see langword="null"/> for a usable style.</summary>
    public UsageError? ValidationError()
    {
        if (LineBreak is not (Lf or Crlf))
            return new UsageError(UsageErrorKind.Argument, "The line break is \\n or \\r\\n.", "value");
        if (Indentation.Length == 0 || Indentation.Any(c => c is not (' ' or '\t')))
            return new UsageError(UsageErrorKind.Argument, "The indentation is spaces or tabs.", "value");
        if (BlockSpacing < 0)
            return new UsageError(UsageErrorKind.Argument, "The spacing before a block is 0 or more empty lines.", "value");
        return null;
    }
}

/// <summary>How a block is written.</summary>
internal enum Braces
{
    /// <summary><c>{</c> at the end of the block's line, each directive on a line of its own.</summary>
    EndOfLine,

    /// <summary><c>{</c> on a line of its own.</summary>
    NewLine,

    /// <summary>The whole block on one line.</summary>
    OneLine,
}
