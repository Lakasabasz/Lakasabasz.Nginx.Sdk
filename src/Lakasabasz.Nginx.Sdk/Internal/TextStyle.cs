namespace Lakasabasz.Nginx.Sdk.Internal;

/// <summary>How new text of a file is written: read from the file, the syntax tree writes by it.</summary>
/// <param name="LineBreak">The line break: <c>\n</c> or <c>\r\n</c>.</param>
/// <param name="Indentation">One level of indentation, spaces or tabs.</param>
internal sealed record TextStyle(string LineBreak, string Indentation)
{
    /// <summary>Line breaks <c>\n</c> and four spaces.</summary>
    public static TextStyle Default { get; } = new("\n", "    ");
}
