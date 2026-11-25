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

    /// <summary>The structure of a piece of nginx text, e.g. pasted into a section: its directives, not linked to any tree.</summary>
    public static IReadOnlyList<StructureDirective> DirectivesOf(SyntaxBlock root) => Bind(root).Structure.Directives;

    /// <summary>Applies the changes of the structure since the last call to the tree.</summary>
    public void Apply(SyntaxBlock root, Formatting formatting) => Apply(root, Structure.TakeChanges(), formatting, ownerIndentation: null);

    private StructureBlock StructureOf(SyntaxBlock block)
    {
        var directives = new List<StructureDirective>();
        foreach (var line in block.Lines)
        {
            for (var i = 0; i < line.Items.Count; i++)
            {
                if (line.Items[i].Node is not SyntaxDirective node)
                    continue;
                var kind = node.Block is not null ? DirectiveKind.Block : node.Raw is not null ? DirectiveKind.Raw : DirectiveKind.Simple;
                var directive = new StructureDirective(node.Name, node.Arguments, kind,
                    node.Block is { } nested ? StructureOf(nested) : null, node.Raw, node.Line, node.Column);
                var comment = i + 1 < line.Items.Count && line.Items[i + 1].Node is SyntaxComment after ? TextOf(after) : null;
                directive.Read(comment, StyleOf(node), TextOf(node));
                _nodes[directive] = node;
                directives.Add(directive);
            }
        }
        return new StructureBlock(directives);
    }

    private void Apply(SyntaxBlock block, BlockChanges changes, Formatting formatting, string? ownerIndentation)
    {
        block.Remove(new HashSet<SyntaxDirective>(changes.Removed.Select(directive => _nodes[directive])), ownerIndentation);
        foreach (var directive in changes.Removed)
            Forget(directive);

        foreach (var directive in changes.Changed)
        {
            var node = _nodes[directive];
            node.Header = HeaderOf(directive, KeptBraceSpacing(node));
            node.Arguments = directive.Arguments;
        }

        foreach (var directive in changes.Commented)
            block.SetTrailingComment(_nodes[directive], LineOf(directive.Comment), formatting, ownerIndentation);

        foreach (var directive in changes.Restyled)
        {
            var node = _nodes[directive];
            var indentation = block.IndentationOf(node);
            node.Header = node.Header.TrimEnd() + BraceSpacing(directive.Style, formatting, indentation);
            node.Block?.Restyle(directive.Style.OneLine, formatting, indentation);
        }

        foreach (var (directive, nested) in changes.Nested)
        {
            var node = _nodes[directive];
            Apply(node.Block!, nested, formatting, block.IndentationOf(node));
        }

        var holdsBlock = block.HoldsBlock;
        foreach (var added in changes.Added)
        {
            Add(block, added, formatting, ownerIndentation, holdsBlock);
            holdsBlock |= added is StructureDirective { IsBlock: true };
        }
    }

    /// <summary>A new directive or comment after the last directive of the block; a comment always ends its line.</summary>
    private void Add(SyntaxBlock block, StructureNode added, Formatting formatting, string? ownerIndentation, bool holdsBlock)
    {
        if (added is StructureComment comment)
        {
            var node = new SyntaxComment(LineOf(comment.Text)!);
            block.Append((_, _) => node, formatting, ownerIndentation, holdsBlock: true);
            block.EndLine(node, formatting, ownerIndentation);
            return;
        }

        var directive = (StructureDirective)added;
        SyntaxDirective? created = null;
        block.Append((indentation, oneLine) => created = Create(directive, formatting, indentation, oneLine), formatting, ownerIndentation, holdsBlock);
        if (directive.Comment is not null)
            block.SetTrailingComment(created!, LineOf(directive.Comment), formatting, ownerIndentation);
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
        {
            var holdsBlock = false;
            foreach (var child in directive.Block.Nodes)
            {
                Add(block, child, formatting, blockOnOneLine ? string.Empty : indentation, holdsBlock);
                holdsBlock |= child is StructureDirective { IsBlock: true };
            }
        }
        return node;
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
        ConfigWriter.AppendHeader(header, directive.Name, directive.Arguments, block: false);
        return header.Append(directive.IsBlock ? braceSpacing ?? " " : string.Empty).ToString();
    }

    /// <summary>The spacing before <c>{</c> that a rewritten header keeps: a line break, when the brace was on a line of its own.</summary>
    private static string? KeptBraceSpacing(SyntaxDirective node)
    {
        if (node.Block is null)
            return null;
        var spacing = node.Header.Substring(node.Header.TrimEnd().Length);
        return spacing.IndexOf('\n') >= 0 ? spacing : null;
    }

    private static string BraceSpacing(BlockStyle style, Formatting formatting, string indentation) =>
        style.BraceOnNewLine ? formatting.Newline + indentation : " ";

    private static BlockStyle StyleOf(SyntaxDirective node) => node.Block is { } block
        ? new BlockStyle(block.IsOneLine, node.Header.Substring(node.Header.TrimEnd().Length).IndexOf('\n') >= 0)
        : BlockStyle.Default;

    /// <summary>A comment's text without <c>#</c> and the space after it.</summary>
    private static string TextOf(SyntaxComment comment)
    {
        var text = comment.Text.Substring(1);
        return text.StartsWith(" ", StringComparison.Ordinal) ? text.Substring(1) : text;
    }

    private static string TextOf(SyntaxDirective node)
    {
        var text = new StringBuilder();
        node.Render(text);
        return text.ToString();
    }

    /// <summary>The comment as written: <c>#</c>, a space and the text.</summary>
    private static string? LineOf(string? comment) => comment is null
        ? null
        : comment.Length == 0 ? CommentStart.ToString() : CommentStart + " " + comment;
}
