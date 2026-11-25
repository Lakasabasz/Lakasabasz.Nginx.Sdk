namespace Lakasabasz.Nginx.Sdk.Internal.Syntax;

/// <summary>The style a file is written in, read from its text: in each field, what most of the file does.</summary>
internal static class StyleDetection
{
    private const string Lf = "\n";
    private const string Crlf = "\r\n";
    private const string FourSpaces = "    ";

    /// <summary>
    /// The more common line break among the lines that end with one (LF on a tie, or with none); the most common step of
    /// indentation from a block's line to the first directive in it, when the step is spaces only or tabs only; the most
    /// common braces of the blocks and the most common number of empty lines before a block that follows another
    /// directive (the defaults when the file has none).
    /// </summary>
    public static TextStyle Of(string text, SyntaxBlock root)
    {
        var lines = text.Split('\n');
        var terminated = lines.Take(lines.Length - 1).ToList();
        var crlfCount = terminated.Count(line => line.EndsWith("\r", StringComparison.Ordinal));
        var blocks = BlocksIn(root).ToList();
        return new TextStyle(
            LineBreak: crlfCount > terminated.Count - crlfCount ? Crlf : Lf,
            Indentation: MostCommon(blocks.Select(block => block.IndentUnit).OfType<string>(), FourSpaces),
            Braces: MostCommon(blocks.Select(block => BracesOf(block.Node)), TextStyle.Default.Braces),
            BlockSpacing: MostCommon(blocks.Where(block => block.FollowsDirective).Select(block => block.Spacing), TextStyle.Default.BlockSpacing));
    }

    /// <summary>
    /// How a block is written: on one line when its body is (also with <c>{</c> under its name, and <c>{}</c>), else
    /// with <c>{</c> on a line of its own or at the end of the line.
    /// </summary>
    public static Braces BracesOf(SyntaxDirective node) => node.Block switch
    {
        { IsOneLine: true } => Braces.OneLine,
        _ when node.BraceOnNewLine => Braces.NewLine,
        _ => Braces.EndOfLine,
    };

    /// <summary>
    /// Every block at any depth, with the empty lines above it (above its comment), whether a directive comes before it
    /// in its block, and its step of indentation: from its line's indentation to that of the first directive in it
    /// (<see langword="null"/> when that is on the same line, or not a whole number of spaces or tabs).
    /// </summary>
    private static IEnumerable<(SyntaxDirective Node, int Spacing, bool FollowsDirective, string? IndentUnit)> BlocksIn(SyntaxBlock block)
    {
        for (var i = 0; i < block.Lines.Count; i++)
        {
            var line = block.Lines[i];
            foreach (var node in line.Items.Select(item => item.Node).OfType<SyntaxDirective>().Where(node => node.Block is not null))
            {
                var commentTop = i;
                while (commentTop > 0 && block.Lines[commentTop - 1].IsCommentLine)
                    commentTop--;
                var spacing = 0;
                while (commentTop - spacing > 0 && block.Lines[commentTop - spacing - 1].IsBlank)
                    spacing++;
                yield return (node, spacing, block.Lines.Take(i).Any(earlier => earlier.HasDirectives), IndentUnitOf(node.Block!, line.Indentation));
                foreach (var inner in BlocksIn(node.Block!))
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
