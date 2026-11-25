using System.Text;
using Lakasabasz.Nginx.Sdk.Internal.Structure;

namespace Lakasabasz.Nginx.Sdk.Internal.Syntax;

/// <summary>
/// The link of a syntax tree to its internal structure: builds the structure from the tree and turns the changes the
/// structure reports into edits of the tree. The only place that knows both; the structure knows neither.
/// </summary>
/// <remarks>
/// Comments are read by fixed rules: one after a simple directive on its line is the directive's; the comment lines
/// right above a block (no empty line between) are the block's; the comment lines at the top of a file followed by an
/// empty line are the file's. Any other comment stays where it is and belongs to nothing.
/// </remarks>
internal sealed class SyntaxBinding
{
    private const char CommentStart = '#';
    private const char CommentLineSeparator = '\n';

    private readonly Dictionary<StructureDirective, SyntaxDirective> _nodes = [];
    private readonly Dictionary<StructureDirective, List<SyntaxLine>> _commentLines = [];
    /// <summary>The lines of the file's comment and the empty line after it.</summary>
    private List<SyntaxLine> _fileHeaderLines = [];

    private SyntaxBinding()
    {
        Structure = new StructureBlock([]);
    }

    public StructureBlock Structure { get; private set; }

    /// <summary>The structure of a tree written in <paramref name="style"/>: every directive with its comment and block style, its node remembered for later edits.</summary>
    public static SyntaxBinding Bind(SyntaxBlock root, TextStyle style)
    {
        var binding = new SyntaxBinding();
        binding.Structure = new StructureBlock(binding.NodesOf(root, style, fromFile: true));
        binding.Structure.ReadStyle(style);
        binding.Structure.ReadComment(binding.ReadFileComment(root));
        return binding;
    }

    /// <summary>
    /// The directives and comments of a piece of nginx text written in <paramref name="style"/>, e.g. pasted into a
    /// section: added in code, so with no place in a file and no tree of their own; a block takes them by being made of them.
    /// </summary>
    public static List<StructureNode> Fragment(SyntaxBlock root, TextStyle style) => new SyntaxBinding().NodesOf(root, style, fromFile: false);

    /// <summary>Applies the changes of the structure since the last call to the tree.</summary>
    public void Apply(SyntaxBlock root)
    {
        var changes = Structure.TakeChanges();
        if (changes.OwnCommentChanged)
            SetFileComment(root, Structure.Comment, Structure.Style);
        Apply(root, changes, Structure.Style, ownerIndentation: null);
    }

    /// <summary>
    /// Every directive with its comment (see the remarks of the class), and the comments that belong to nothing. From a
    /// file, a directive keeps its place and text, and its node and comment lines are remembered for later edits.
    /// </summary>
    private List<StructureNode> NodesOf(SyntaxBlock block, TextStyle style, bool fromFile)
    {
        var nodes = new List<StructureNode>();
        var commentLines = new List<SyntaxLine>();
        foreach (var line in block.Lines)
        {
            if (line.IsCommentLine)
            {
                commentLines.Add(line);
                continue;
            }

            var blockComment = line.Items.Count > 0 && line.Items[0].Node is SyntaxDirective { HasBody: true } ? commentLines.ToList() : [];
            nodes.AddRange(commentLines.Except(blockComment).Select(comment => new StructureComment(CommentTextOf((SyntaxComment)comment.Items[0].Node))));
            commentLines.Clear();

            for (var i = 0; i < line.Items.Count; i++)
            {
                if (line.Items[i].Node is not SyntaxDirective node)
                    continue;
                var directive = DirectiveOf(node, style, fromFile);
                directive.Read(CommentOf(line, i, blockComment), fromFile ? SourceTextOf(node) : null);
                if (fromFile)
                {
                    _nodes[directive] = node;
                    if (i == 0 && node.HasBody)
                        _commentLines[directive] = blockComment;
                }
                nodes.Add(directive);
            }
        }

        nodes.AddRange(commentLines.Select(comment => new StructureComment(CommentTextOf((SyntaxComment)comment.Items[0].Node))));
        return nodes;
    }

    /// <summary>
    /// The comment of the directive at <paramref name="index"/> of the line: for a block starting the line, the comment
    /// lines above it; for a simple directive, the comment after it on the line.
    /// </summary>
    private static string? CommentOf(SyntaxLine line, int index, List<SyntaxLine> commentLinesAbove)
    {
        var node = (SyntaxDirective)line.Items[index].Node;
        if (node.HasBody)
            return index == 0 && commentLinesAbove.Count > 0 ? JoinedComment(commentLinesAbove) : null;
        return index + 1 < line.Items.Count && line.Items[index + 1].Node is SyntaxComment after ? CommentTextOf(after) : null;
    }

    /// <summary>A directive of the tree, with its block: written in its own braces and the file's other style.</summary>
    private StructureDirective DirectiveOf(SyntaxDirective node, TextStyle style, bool fromFile)
    {
        var kind = KindOf(node);
        var nested = node.Block is { } inner ? new StructureBlock(NodesOf(inner, style, fromFile)) : null;
        var directive = fromFile
            ? new StructureDirective(node.Name, node.Arguments, kind, nested, node.Raw, node.Line, node.Column)
            : new StructureDirective(node.Name, node.Arguments, kind, nested, node.Raw);
        if (node.Block is not null)
            directive.Block.ReadStyle(style with { Braces = StyleDetection.BracesOf(node) });
        return directive;
    }

    /// <summary>The comment lines at the top of a file followed by an empty line, remembered for later edits.</summary>
    private string? ReadFileComment(SyntaxBlock root)
    {
        var comment = root.Lines.TakeWhile(line => line.IsCommentLine).ToList();
        if (comment.Count == 0 || comment.Count >= root.Lines.Count || !root.Lines[comment.Count].IsBlank)
            return null;
        _fileHeaderLines = [.. comment, root.Lines[comment.Count]];
        return JoinedComment(comment);
    }

    private void Apply(SyntaxBlock block, BlockChanges changes, TextStyle style, string? ownerIndentation)
    {
        foreach (var directive in changes.Removed.Where(directive => directive.IsBlock))
            block.RemoveLinesAbove(_nodes[directive], CommentLinesOf(directive), _fileHeaderLines);
        block.Remove(new HashSet<SyntaxDirective>(changes.Removed.Select(directive => _nodes[directive])), ownerIndentation);
        foreach (var directive in changes.Removed)
            Forget(directive);

        foreach (var directive in changes.ArgumentsChanged)
        {
            var node = _nodes[directive];
            node.Header = HeaderOf(directive, KeptBraceSpacing(node));
            node.Arguments = directive.Arguments;
        }

        foreach (var directive in changes.CommentChanged)
            SetComment(block, directive, _nodes[directive], style, ownerIndentation);

        foreach (var directive in changes.StyleChanged)
        {
            var node = _nodes[directive];
            var indentation = block.IndentationOf(node);
            var blockStyle = directive.Block.EffectiveStyle;
            node.Header = node.Header.TrimEnd() + BraceSpacing(blockStyle, indentation);
            node.Block?.Restyle(blockStyle.Braces == Braces.OneLine, blockStyle, indentation);
            NoteBracesAsWritten(directive, node);
        }

        foreach (var (directive, nested) in changes.Nested)
        {
            var node = _nodes[directive];
            Apply(node.Block!, nested, directive.Block.EffectiveStyle, block.IndentationOf(node));
            NoteBracesAsWritten(directive, node);
        }

        AddAll(block, changes.Added, style, ownerIndentation, block.HoldsBlock);
    }

    /// <summary>New directives and comments in order, each after the one before.</summary>
    private void AddAll(SyntaxBlock block, IEnumerable<StructureNode> nodes, TextStyle style, string? ownerIndentation, bool holdsBlock)
    {
        SyntaxNode? previous = null;
        foreach (var added in nodes)
        {
            previous = Add(block, added, style, ownerIndentation, holdsBlock, previous);
            holdsBlock |= added is StructureDirective { IsBlock: true };
        }
    }

    /// <summary>
    /// A new directive or comment after <paramref name="previous"/>, or after the last directive of the block. A block
    /// gets its comment right above it and the spacing of <paramref name="style"/> above that; a block, a comment or a
    /// commented directive puts a block written on one line onto lines first.
    /// </summary>
    /// <returns>Its node.</returns>
    private SyntaxNode Add(SyntaxBlock block, StructureNode added, TextStyle style, string? ownerIndentation, bool holdsBlock, SyntaxNode? previous)
    {
        if (added is StructureComment comment)
        {
            var node = new SyntaxComment(CommentAsWritten(comment.Text));
            block.Append((_, _) => node, style, ownerIndentation, holdsBlock: true, previous);
            block.BreakLineBeforeClosingBrace(node, style, ownerIndentation);
            return node;
        }

        var directive = (StructureDirective)added;
        var onLines = holdsBlock || directive.IsBlock || directive.Comment is not null;
        var created = (SyntaxDirective)block.Append(
            (indentation, oneLine) => Create(directive, style, indentation, oneLine), style, ownerIndentation, onLines, previous);
        if (directive.IsBlock)
        {
            SetComment(block, directive, created, style, ownerIndentation);
            block.SpaceAbove(_commentLines.TryGetValue(directive, out var lines) && lines.Count > 0 ? lines[0] : block.LineOf(created),
                style.BlockSpacing, style);
        }
        else if (directive.Comment is not null)
        {
            block.SetTrailingComment(created, CommentAsWritten(directive.Comment), style, ownerIndentation);
        }
        return created;
    }

    /// <summary>The node of a new directive; a block gets its directives, on one line or on lines of their own.</summary>
    private SyntaxDirective Create(StructureDirective directive, TextStyle style, string indentation, bool oneLine)
    {
        SyntaxBlock? block = null;
        var blockStyle = directive.Block.EffectiveStyle;
        var blockOnOneLine = oneLine || (blockStyle.Braces == Braces.OneLine && CanBeOneLine(directive.Block));
        if (directive.IsBlock)
        {
            block = blockOnOneLine
                ? SyntaxBlock.PrepareBetweenBraces(string.Empty, [], " ")
                : SyntaxBlock.PrepareBetweenBraces(style.LineBreak, [], indentation);
        }

        var header = directive.IsBlock ? HeaderOf(directive, BraceSpacing(blockStyle, indentation)) : HeaderOf(directive, null);
        var node = new SyntaxDirective(directive.Name, directive.Arguments, header, block, null);
        _nodes[directive] = node;
        if (block is not null)
            AddAll(block, directive.Block.Nodes, blockStyle, blockOnOneLine ? string.Empty : indentation, holdsBlock: false);
        NoteBracesAsWritten(directive, node);
        return node;
    }

    /// <summary>
    /// Sets the comment of a directive: at the end of its line for a simple one, on the lines right above it for a block.
    /// A block written on one line goes onto lines first, so the comment comments out nothing and its lines can be above.
    /// </summary>
    private void SetComment(SyntaxBlock block, StructureDirective directive, SyntaxDirective node, TextStyle style, string? ownerIndentation)
    {
        if (ownerIndentation is not null && block.IsOneLine && (directive.IsBlock || directive.Comment is not null))
            block.Restyle(oneLine: false, style, ownerIndentation);
        if (!directive.IsBlock)
        {
            block.SetTrailingComment(node, directive.Comment is null ? null : CommentAsWritten(directive.Comment), style, ownerIndentation);
            return;
        }

        var lines = CommentLines(directive.Comment, block.IndentationOf(node), style);
        block.ReplaceLinesAbove(node, CommentLinesOf(directive), lines);
        _commentLines[directive] = lines;
    }

    /// <summary>Sets the comment at the top of the file: its lines and one empty line after them.</summary>
    private void SetFileComment(SyntaxBlock root, string? comment, TextStyle style)
    {
        var lines = CommentLines(comment, string.Empty, style);
        if (lines.Count > 0)
            lines.Add(SyntaxLine.Blank(style.LineBreak));
        root.ReplaceFileHeader(_fileHeaderLines, lines);
        _fileHeaderLines = lines;
    }

    /// <summary>The lines of a comment, each <c># text</c> with the indentation given; none for no comment.</summary>
    private static List<SyntaxLine> CommentLines(string? comment, string indentation, TextStyle style)
    {
        if (comment is null)
            return [];
        return comment.Split(CommentLineSeparator)
            .Select(text => SyntaxLine.Holding(new SyntaxComment(CommentAsWritten(text)), indentation, style.LineBreak))
            .ToList();
    }

    private List<SyntaxLine> CommentLinesOf(StructureDirective directive) =>
        _commentLines.TryGetValue(directive, out var lines) ? lines : [];

    /// <summary>A block can be written on one line when it holds no block and no comment.</summary>
    private static bool CanBeOneLine(StructureBlock block) => block.Nodes.All(node =>
        node is StructureDirective { IsBlock: false, Comment: null });

    /// <summary>
    /// The structure learns the braces the block is written with, when it is not on one line although its style says
    /// so: a block holding a block or a comment is written on lines.
    /// </summary>
    private static void NoteBracesAsWritten(StructureDirective directive, SyntaxDirective node)
    {
        if (directive.Block.Style.Braces == Braces.OneLine && node.Block is { IsOneLine: false })
            directive.Block.NoteWrittenBraces(StyleDetection.BracesOf(node));
    }

    private void Forget(StructureDirective directive)
    {
        _nodes.Remove(directive);
        _commentLines.Remove(directive);
        foreach (var child in directive.Block.Directives)
            Forget(child);
    }

    /// <summary>The name and arguments; a block's header ends with the spacing before its <c>{</c>.</summary>
    private static string HeaderOf(StructureDirective directive, string? braceSpacing)
    {
        var header = new StringBuilder();
        ConfigWriter.AppendHeader(header, directive.Name, directive.Arguments);
        return header.Append(directive.IsBlock ? braceSpacing ?? " " : string.Empty).ToString();
    }

    /// <summary>The spacing before <c>{</c> that a rewritten header keeps: a line break, when the brace was on a line of its own.</summary>
    private static string? KeptBraceSpacing(SyntaxDirective node)
    {
        if (node.Block is null)
            return null;
        return node.BraceOnNewLine ? node.BraceSpacing : null;
    }

    /// <summary>The spacing before <c>{</c>: a line break and the block's indentation for a brace on a line of its own, else a space.</summary>
    private static string BraceSpacing(TextStyle style, string indentation) =>
        style.Braces == Braces.NewLine ? style.LineBreak + indentation : " ";

    private static DirectiveKind KindOf(SyntaxDirective node) => (node.Block, node.Raw) switch
    {
        (not null, _) => DirectiveKind.Block,
        (_, not null) => DirectiveKind.Raw,
        _ => DirectiveKind.Simple,
    };

    /// <summary>The comment of several lines, each without <c>#</c> and the space after it, joined by <c>\n</c>.</summary>
    private static string JoinedComment(IEnumerable<SyntaxLine> lines) => string.Join(
        CommentLineSeparator.ToString(), lines.Select(line => CommentTextOf((SyntaxComment)line.Items[0].Node)));

    /// <summary>A comment's text without <c>#</c> and the space after it.</summary>
    private static string CommentTextOf(SyntaxComment comment)
    {
        var text = comment.Text.Substring(1);
        return text.StartsWith(" ", StringComparison.Ordinal) ? text.Substring(1) : text;
    }

    /// <summary>One line of a comment as written: <c>#</c>, a space and the text; <c>#</c> alone for an empty one.</summary>
    private static string CommentAsWritten(string text) => text.Length == 0 ? CommentStart.ToString() : CommentStart + " " + text;

    /// <summary>The directive as written in the file, with its block.</summary>
    private static string SourceTextOf(SyntaxDirective node)
    {
        var text = new StringBuilder();
        node.Render(text);
        return text.ToString();
    }
}
