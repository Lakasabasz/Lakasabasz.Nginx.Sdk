using System.Text;
using Lakasabasz.Nginx.Sdk.Internal.Structure;

namespace Lakasabasz.Nginx.Sdk.Internal.Syntax;

/// <summary>
/// The link of a syntax tree to its internal structure: builds the structure from the tree and turns the changes the
/// structure reports into edits of the tree. The only place that knows both; the structure knows neither.
/// </summary>
internal sealed class SyntaxBinding
{
    private const char CommentStart = '#';

    private readonly Dictionary<StructureDirective, SyntaxDirective> _nodes = [];

    private SyntaxBinding()
    {
        Structure = new StructureBlock([]);
    }

    public StructureBlock Structure { get; private set; }

    /// <summary>The structure of a tree: every directive with its comment and block style, its node remembered for later edits.</summary>
    public static SyntaxBinding Bind(SyntaxBlock root)
    {
        var binding = new SyntaxBinding();
        binding.Structure = binding.StructureOf(root);
        return binding;
    }

    /// <summary>
    /// The directives and comments of a piece of nginx text, e.g. pasted into a section: added in code, so with no
    /// place in a file and no tree of their own; a block takes them by being made of them.
    /// </summary>
    public static List<StructureNode> Fragment(SyntaxBlock root) => new SyntaxBinding().NodesOf(root, fromFile: false);

    /// <summary>Applies the changes of the structure since the last call to the tree.</summary>
    public void Apply(SyntaxBlock root, Formatting formatting) => Apply(root, Structure.TakeChanges(), formatting, ownerIndentation: null);

    private StructureBlock StructureOf(SyntaxBlock block) => new(NodesOf(block, fromFile: true));

    /// <summary>
    /// Every directive with the comment after it on its line, and the comments on lines of their own. From a file, a
    /// directive keeps its place and text, and its node is remembered for later edits.
    /// </summary>
    private List<StructureNode> NodesOf(SyntaxBlock block, bool fromFile)
    {
        var nodes = new List<StructureNode>();
        foreach (var line in block.Lines)
        {
            for (var i = 0; i < line.Items.Count; i++)
            {
                var item = line.Items[i].Node;
                if (item is SyntaxComment standalone && (i == 0 || line.Items[i - 1].Node is not SyntaxDirective))
                    nodes.Add(new StructureComment(CommentTextOf(standalone)));
                if (item is not SyntaxDirective node)
                    continue;

                var kind = KindOf(node);
                var nested = node.Block is { } inner ? new StructureBlock(NodesOf(inner, fromFile)) : null;
                var directive = fromFile
                    ? new StructureDirective(node.Name, node.Arguments, kind, nested, node.Raw, node.Line, node.Column)
                    : new StructureDirective(node.Name, node.Arguments, kind, nested, node.Raw);
                var comment = i + 1 < line.Items.Count && line.Items[i + 1].Node is SyntaxComment after ? CommentTextOf(after) : null;
                directive.Read(comment, StyleOf(node), fromFile ? SourceTextOf(node) : null);
                if (fromFile)
                    _nodes[directive] = node;
                nodes.Add(directive);
            }
        }
        return nodes;
    }

    private void Apply(SyntaxBlock block, BlockChanges changes, Formatting formatting, string? ownerIndentation)
    {
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
            block.SetTrailingComment(_nodes[directive], CommentAsWritten(directive.Comment), formatting, ownerIndentation);

        foreach (var directive in changes.StyleChanged)
        {
            var node = _nodes[directive];
            var indentation = block.IndentationOf(node);
            node.Header = node.Header.TrimEnd() + BraceSpacing(directive.Style, formatting, indentation);
            node.Block?.Restyle(directive.Style.OneLine, formatting, indentation);
            NoteUnfolded(directive, node);
        }

        foreach (var (directive, nested) in changes.Nested)
        {
            var node = _nodes[directive];
            Apply(node.Block!, nested, formatting, block.IndentationOf(node));
            NoteUnfolded(directive, node);
        }

        AddAll(block, changes.Added, formatting, ownerIndentation, block.HoldsBlock);
    }

    /// <summary>New directives and comments in order, each after the one before.</summary>
    private void AddAll(SyntaxBlock block, IEnumerable<StructureNode> nodes, Formatting formatting, string? ownerIndentation, bool holdsBlock)
    {
        SyntaxNode? previous = null;
        foreach (var added in nodes)
        {
            previous = Add(block, added, formatting, ownerIndentation, holdsBlock, previous);
            holdsBlock |= added is StructureDirective { IsBlock: true };
        }
    }

    /// <summary>
    /// A new directive or comment after <paramref name="previous"/>, or after the last directive of the block; a comment
    /// always ends its line.
    /// </summary>
    /// <returns>Its node.</returns>
    private SyntaxNode Add(SyntaxBlock block, StructureNode added, Formatting formatting, string? ownerIndentation, bool holdsBlock, SyntaxNode? previous)
    {
        if (added is StructureComment comment)
        {
            var node = new SyntaxComment(CommentAsWritten(comment.Text)!);
            block.Append((_, _) => node, formatting, ownerIndentation, holdsBlock: true, previous);
            block.BreakLineBeforeClosingBrace(node, formatting, ownerIndentation);
            return node;
        }

        var directive = (StructureDirective)added;
        var created = (SyntaxDirective)block.Append(
            (indentation, oneLine) => Create(directive, formatting, indentation, oneLine), formatting, ownerIndentation, holdsBlock, previous);
        if (directive.Comment is not null)
            block.SetTrailingComment(created, CommentAsWritten(directive.Comment), formatting, ownerIndentation);
        return created;
    }

    /// <summary>The node of a new directive; a block gets its directives, on one line or on lines of their own.</summary>
    private SyntaxDirective Create(StructureDirective directive, Formatting formatting, string indentation, bool oneLine)
    {
        SyntaxBlock? block = null;
        var blockOnOneLine = oneLine || directive.Style.OneLine;
        if (directive.IsBlock)
        {
            block = blockOnOneLine
                ? new SyntaxBlock { BeforeClosing = " " }
                : new SyntaxBlock { AfterOpening = formatting.Newline, BeforeClosing = indentation };
        }

        var header = directive.IsBlock ? HeaderOf(directive, BraceSpacing(directive.Style, formatting, indentation)) : HeaderOf(directive, null);
        var node = new SyntaxDirective(directive.Name, directive.Arguments, header, block, null);
        _nodes[directive] = node;
        if (block is not null)
            AddAll(block, directive.Block.Nodes, formatting, blockOnOneLine ? string.Empty : indentation, holdsBlock: false);
        NoteUnfolded(directive, node);
        return node;
    }

    /// <summary>
    /// The structure learns that its block is not on one line, when the block could not be or no longer is: one holding
    /// a block or a comment is written on lines.
    /// </summary>
    private static void NoteUnfolded(StructureDirective directive, SyntaxDirective node)
    {
        if (directive.Style.OneLine && node.Block is { IsOneLine: false })
            directive.NoteWrittenStyle(directive.Style with { OneLine = false });
    }

    private void Forget(StructureDirective directive)
    {
        _nodes.Remove(directive);
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
        var spacing = BraceSpacingOf(node);
        return spacing.IndexOf('\n') >= 0 ? spacing : null;
    }

    private static string BraceSpacing(BlockStyle style, Formatting formatting, string indentation) =>
        style.BraceOnNewLine ? formatting.Newline + indentation : " ";

    private static BlockStyle StyleOf(SyntaxDirective node) => node.Block is { } block
        ? new BlockStyle(block.IsOneLine, BraceSpacingOf(node).IndexOf('\n') >= 0)
        : BlockStyle.Default;

    private static DirectiveKind KindOf(SyntaxDirective node) => (node.Block, node.Raw) switch
    {
        (not null, _) => DirectiveKind.Block,
        (_, not null) => DirectiveKind.Raw,
        _ => DirectiveKind.Simple,
    };

    /// <summary>The spacing at the end of a block's header, before its <c>{</c>.</summary>
    private static string BraceSpacingOf(SyntaxDirective node) => node.Header.Substring(node.Header.TrimEnd().Length);

    /// <summary>A comment's text without <c>#</c> and the space after it.</summary>
    private static string CommentTextOf(SyntaxComment comment)
    {
        var text = comment.Text.Substring(1);
        return text.StartsWith(" ", StringComparison.Ordinal) ? text.Substring(1) : text;
    }

    /// <summary>The directive as written in the file, with its block.</summary>
    private static string SourceTextOf(SyntaxDirective node)
    {
        var text = new StringBuilder();
        node.Render(text);
        return text.ToString();
    }

    /// <summary>The comment as written: <c>#</c>, a space and the text.</summary>
    private static string? CommentAsWritten(string? comment) => comment is null
        ? null
        : comment.Length == 0 ? CommentStart.ToString() : CommentStart + " " + comment;
}
