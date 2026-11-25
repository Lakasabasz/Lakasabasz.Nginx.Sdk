namespace Lakasabasz.Nginx.Sdk.Internal.Syntax;

/// <summary>The style a file is written in, read from its text: in each field, what most of the file does.</summary>
internal static class StyleDetection
{
    private const string Lf = "\n";
    private const string Crlf = "\r\n";
    private const string FourSpaces = "    ";

    /// <summary>
    /// The more common line break among the lines that end with one (LF on a tie, or with none), and the most common
    /// step of indentation from a block's line to the first directive in it, when the step is spaces only or tabs only
    /// (four spaces when the file has none).
    /// </summary>
    public static TextStyle Of(string text, SyntaxBlock root)
    {
        var lines = text.Split('\n');
        var terminated = lines.Take(lines.Length - 1).ToList();
        var crlfCount = terminated.Count(line => line.EndsWith("\r", StringComparison.Ordinal));
        return new TextStyle(
            LineBreak: crlfCount > terminated.Count - crlfCount ? Crlf : Lf,
            Indentation: MostCommon(IndentUnitsIn(root).OfType<string>(), FourSpaces));
    }

    /// <summary>
    /// The step of indentation of every block at any depth: from its line's indentation to that of the first directive
    /// in it (<see langword="null"/> when that is on the same line, or not a whole number of spaces or tabs).
    /// </summary>
    private static IEnumerable<string?> IndentUnitsIn(SyntaxBlock block)
    {
        foreach (var line in block.Lines)
        {
            foreach (var node in line.Items.Select(item => item.Node).OfType<SyntaxDirective>().Where(node => node.Block is not null))
            {
                yield return IndentUnitOf(node.Block!, line.Indentation);
                foreach (var inner in IndentUnitsIn(node.Block!))
                    yield return inner;
            }
        }
    }

    /// <summary>The step from <paramref name="owner"/> to the first directive line of the block, when it is spaces only or tabs only.</summary>
    private static string? IndentUnitOf(SyntaxBlock block, string owner)
    {
        if (block.IsOneLine || block.Lines.FirstOrDefault(line => line.HasDirectives) is not { } first)
            return null;
        var inner = first.Indentation;
        if (!inner.StartsWith(owner, StringComparison.Ordinal) || inner.Length == owner.Length)
            return null;
        var step = inner.Substring(owner.Length);
        return step.All(c => c == ' ') || step.All(c => c == '\t') ? step : null;
    }

    /// <summary>The value seen most often; on a tie, the one seen first; <paramref name="none"/> without values.</summary>
    private static T MostCommon<T>(IEnumerable<T> values, T none) => values
        .GroupBy(value => value)
        .OrderByDescending(group => group.Count())
        .Select(group => group.Key)
        .DefaultIfEmpty(none)
        .First();
}
