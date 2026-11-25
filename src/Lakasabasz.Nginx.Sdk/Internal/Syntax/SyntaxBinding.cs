using System.Text;
using Lakasabasz.Nginx.Sdk.Internal.Structure;

namespace Lakasabasz.Nginx.Sdk.Internal.Syntax;

/// <summary>
/// The link of a syntax tree to its internal structure: builds the structure from the tree and turns the changes the
/// structure reports into edits of the tree. The only place that knows both; the structure knows neither.
/// </summary>
internal sealed class SyntaxBinding
{
    private readonly Dictionary<StructureDirective, SyntaxDirective> _nodes = [];
    private readonly TextStyle _style;

    private SyntaxBinding(TextStyle style)
    {
        _style = style;
        Structure = new StructureBlock([]);
    }

    public StructureBlock Structure { get; private set; }

    /// <summary>The structure of a tree written in <paramref name="style"/>: every directive, its node remembered for later edits.</summary>
    public static SyntaxBinding Bind(SyntaxBlock root, TextStyle style)
    {
        var binding = new SyntaxBinding(style);
        binding.Structure = new StructureBlock(binding.DirectivesOf(root, fromFile: true));
        return binding;
    }

    /// <summary>
    /// The directives of a piece of nginx text, e.g. pasted into a section: added in code, so with no place in a file
    /// and no tree of their own; a block takes them by being made of them.
    /// </summary>
    public static List<StructureDirective> Fragment(SyntaxBlock root) => new SyntaxBinding(TextStyle.Default).DirectivesOf(root, fromFile: false);

    /// <summary>Applies the changes of the structure since the last call to the tree.</summary>
    public void Apply(SyntaxBlock root) => Apply(root, Structure.TakeChanges(), ownerIndentation: null);

    /// <summary>
    /// Every directive of the block. From a file, a directive keeps its place and text, and its node is remembered for
    /// later edits.
    /// </summary>
    private List<StructureDirective> DirectivesOf(SyntaxBlock block, bool fromFile)
    {
        var directives = new List<StructureDirective>();
        foreach (var node in block.Lines.SelectMany(line => line.Items).Select(item => item.Node).OfType<SyntaxDirective>())
        {
            var directive = DirectiveOf(node, fromFile);
            directive.Read(fromFile ? SourceTextOf(node) : null);
            if (fromFile)
                _nodes[directive] = node;
            directives.Add(directive);
        }
        return directives;
    }

    /// <summary>A directive of the tree, with its block.</summary>
    private StructureDirective DirectiveOf(SyntaxDirective node, bool fromFile)
    {
        var kind = KindOf(node);
        var nested = node.Block is { } inner ? new StructureBlock(DirectivesOf(inner, fromFile)) : null;
        return fromFile
            ? new StructureDirective(node.Name, node.Arguments, kind, nested, node.Raw, node.Line, node.Column)
            : new StructureDirective(node.Name, node.Arguments, kind, nested, node.Raw);
    }

    private void Apply(SyntaxBlock block, BlockChanges changes, string? ownerIndentation)
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

        foreach (var (directive, nested) in changes.Nested)
        {
            var node = _nodes[directive];
            Apply(node.Block!, nested, block.IndentationOf(node));
        }

        AddAll(block, changes.Added, ownerIndentation, block.HoldsBlock);
    }

    /// <summary>New directives in order, each after the one before.</summary>
    private void AddAll(SyntaxBlock block, IEnumerable<StructureDirective> directives, string? ownerIndentation, bool holdsBlock)
    {
        SyntaxNode? previous = null;
        foreach (var directive in directives)
        {
            previous = block.Append(
                (indentation, oneLine) => Create(directive, indentation, oneLine), _style, ownerIndentation, holdsBlock, previous);
            holdsBlock |= directive.IsBlock;
        }
    }

    /// <summary>The node of a new directive; a block gets its directives, on one line or on lines of their own.</summary>
    private SyntaxDirective Create(StructureDirective directive, string indentation, bool oneLine)
    {
        SyntaxBlock? block = null;
        if (directive.IsBlock)
        {
            block = oneLine
                ? SyntaxBlock.PrepareBetweenBraces(string.Empty, [], " ")
                : SyntaxBlock.PrepareBetweenBraces(_style.LineBreak, [], indentation);
        }

        var node = new SyntaxDirective(directive.Name, directive.Arguments, HeaderOf(directive, null), block, null);
        _nodes[directive] = node;
        if (block is not null)
            AddAll(block, directive.Block.Directives, oneLine ? string.Empty : indentation, holdsBlock: false);
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

    private static DirectiveKind KindOf(SyntaxDirective node) => (node.Block, node.Raw) switch
    {
        (not null, _) => DirectiveKind.Block,
        (_, not null) => DirectiveKind.Raw,
        _ => DirectiveKind.Simple,
    };

    /// <summary>The directive as written in the file, with its block.</summary>
    private static string SourceTextOf(SyntaxDirective node)
    {
        var text = new StringBuilder();
        node.Render(text);
        return text.ToString();
    }
}
