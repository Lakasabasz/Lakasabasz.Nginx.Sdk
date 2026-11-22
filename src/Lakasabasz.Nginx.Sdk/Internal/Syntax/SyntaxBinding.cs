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

    private SyntaxBinding()
    {
        Structure = new StructureBlock([]);
    }

    public StructureBlock Structure { get; private set; }

    /// <summary>The structure of a tree: every directive, with its node remembered for later edits.</summary>
    public static SyntaxBinding Bind(SyntaxBlock root)
    {
        var binding = new SyntaxBinding();
        binding.Structure = binding.StructureOf(root);
        return binding;
    }

    /// <summary>Applies the changes of the structure since the last call to the tree.</summary>
    public void Apply(SyntaxBlock root, Formatting formatting) => Apply(root, Structure.TakeChanges(), formatting, ownerIndentation: null);

    private StructureBlock StructureOf(SyntaxBlock block) => new(block.Lines
        .SelectMany(line => line.Items)
        .Select(item => item.Node)
        .OfType<SyntaxDirective>()
        .Select(node =>
        {
            var kind = node.Block is not null ? DirectiveKind.Block : node.Raw is not null ? DirectiveKind.Raw : DirectiveKind.Simple;
            var directive = new StructureDirective(node.Name, node.Arguments, kind,
                node.Block is { } nested ? StructureOf(nested) : null, node.Raw, node.Line, node.Column);
            _nodes[directive] = node;
            return directive;
        })
        .ToList());

    private void Apply(SyntaxBlock block, BlockChanges changes, Formatting formatting, string? ownerIndentation)
    {
        block.Remove(new HashSet<SyntaxDirective>(changes.Removed.Select(directive => _nodes[directive])), ownerIndentation);
        foreach (var directive in changes.Removed)
            Forget(directive);

        foreach (var directive in changes.Changed)
        {
            var node = _nodes[directive];
            node.Header = HeaderOf(directive);
            node.Arguments = directive.Arguments;
        }

        foreach (var (directive, nested) in changes.Nested)
        {
            var node = _nodes[directive];
            Apply(node.Block!, nested, formatting, block.IndentationOf(node));
        }

        var holdsBlock = block.HoldsBlock;
        foreach (var directive in changes.Added)
        {
            block.Append((indentation, oneLine) => Create(directive, formatting, indentation, oneLine), formatting, ownerIndentation, holdsBlock);
            holdsBlock |= directive.IsBlock;
        }
    }

    /// <summary>The node of a new directive; a block gets its directives, on one line or on lines of their own.</summary>
    private SyntaxDirective Create(StructureDirective directive, Formatting formatting, string indentation, bool oneLine)
    {
        SyntaxBlock? block = null;
        if (directive.IsBlock)
        {
            block = oneLine
                ? new SyntaxBlock { BeforeClosing = " " }
                : new SyntaxBlock { AfterOpening = formatting.Newline, BeforeClosing = indentation };
        }

        var node = new SyntaxDirective(directive.Name, directive.Arguments, HeaderOf(directive), block, null);
        _nodes[directive] = node;
        if (block is not null)
        {
            var holdsBlock = false;
            foreach (var child in directive.Block.Directives)
            {
                block.Append((childIndentation, childOneLine) => Create(child, formatting, childIndentation, childOneLine),
                    formatting, indentation, holdsBlock);
                holdsBlock |= child.IsBlock;
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

    private static string HeaderOf(StructureDirective directive)
    {
        var header = new StringBuilder();
        ConfigWriter.AppendHeader(header, directive.Name, directive.Arguments, directive.IsBlock);
        return header.ToString();
    }
}
