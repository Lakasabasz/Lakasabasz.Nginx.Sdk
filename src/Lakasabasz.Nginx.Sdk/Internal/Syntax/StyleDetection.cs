namespace Lakasabasz.Nginx.Sdk.Internal.Syntax;

/// <summary>The style a file is written in, read from its text: in each field, what most of the file does.</summary>
internal static class StyleDetection
{
    private const string Lf = "\n";
    private const string Crlf = "\r\n";
    private const string Tab = "\t";
    private const string FourSpaces = "    ";

    /// <summary>
    /// The more common line break (LF on a tie); a tab when more lines start with a tab than with a space, else four
    /// spaces (only the kind is read, not the width); the most common braces of the blocks and the most common number
    /// of empty lines before a block that follows another directive (the defaults when the file has none).
    /// </summary>
    public static NginxStyle Of(string text, SyntaxBlock root)
    {
        var lines = text.Split('\n');
        var broken = lines.Take(lines.Length - 1).ToList();
        var crlf = broken.Count(line => line.EndsWith("\r", StringComparison.Ordinal));
        var afterBreak = lines.Skip(1).ToList();
        var tabs = afterBreak.Count(line => line.StartsWith(Tab, StringComparison.Ordinal));
        var spaces = afterBreak.Count(line => line.StartsWith(" ", StringComparison.Ordinal));
        var blocks = BlocksIn(root).ToList();
        return new NginxStyle
        {
            LineBreak = crlf > broken.Count - crlf ? Crlf : Lf,
            Indentation = tabs > spaces ? Tab : FourSpaces,
            Braces = MostCommon(blocks.Select(block => BracesOf(block.Node)), NginxStyle.Default.Braces),
            BlockSpacing = MostCommon(blocks.Where(block => block.FollowsDirective).Select(block => block.Spacing), NginxStyle.Default.BlockSpacing),
        };
    }

    /// <summary>
    /// How a block is written: on one line when its body is (also with <c>{</c> under its name, and <c>{}</c>), else
    /// with <c>{</c> on a line of its own or at the end of the line.
    /// </summary>
    public static BraceStyle BracesOf(SyntaxDirective node) => node.Block switch
    {
        { IsOneLine: true } => BraceStyle.OneLine,
        _ when node.Header.Substring(node.Header.TrimEnd().Length).IndexOf('\n') >= 0 => BraceStyle.NewLine,
        _ => BraceStyle.EndOfLine,
    };

    /// <summary>Every block at any depth, with the empty lines above it (above its comment) and whether a directive comes before it in its block.</summary>
    private static IEnumerable<(SyntaxDirective Node, int Spacing, bool FollowsDirective)> BlocksIn(SyntaxBlock block)
    {
        for (var i = 0; i < block.Lines.Count; i++)
        {
            foreach (var node in block.Lines[i].Items.Select(item => item.Node).OfType<SyntaxDirective>().Where(node => node.Block is not null))
            {
                var top = i;
                while (top > 0 && block.Lines[top - 1].IsCommentLine)
                    top--;
                var spacing = 0;
                while (top - spacing > 0 && block.Lines[top - spacing - 1].IsBlank)
                    spacing++;
                yield return (node, spacing, block.Lines.Take(i).Any(line => line.HasDirectives));
                foreach (var inner in BlocksIn(node.Block!))
                    yield return inner;
            }
        }
    }

    private static T MostCommon<T>(IEnumerable<T> values, T none) => values
        .GroupBy(value => value)
        .OrderByDescending(group => group.Count())
        .Select(group => group.Key)
        .DefaultIfEmpty(none)
        .First();
}
