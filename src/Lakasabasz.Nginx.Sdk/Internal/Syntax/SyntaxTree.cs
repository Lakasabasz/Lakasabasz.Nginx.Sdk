using System.Text;
using Lakasabasz.Nginx.Sdk.Internal.Structure;

namespace Lakasabasz.Nginx.Sdk.Internal.Syntax;

/// <summary>
/// The syntax tree of one file: its lines as written, the formatting new text gets, and, through
/// <see cref="SyntaxBinding"/>, the internal structure read from it. The file side gives it text and takes text back;
/// it never sees the structure.
/// </summary>
internal sealed class SyntaxTree
{
    private const string Include = "include";

    private readonly SyntaxBinding _binding;

    public SyntaxTree(SyntaxBlock root, Formatting formatting)
    {
        Root = root;
        Formatting = formatting;
        _binding = SyntaxBinding.Bind(root);
    }

    public SyntaxBlock Root { get; }

    /// <summary>The line break and indentation unit of the file, for new text.</summary>
    public Formatting Formatting { get; }

    /// <summary>The internal structure of the file: its directives, without formatting.</summary>
    public StructureBlock Structure => _binding.Structure;

    /// <summary>The file's text: the changes of the structure applied, everything unchanged as it was read.</summary>
    public string Render(int capacity)
    {
        _binding.Apply(Root, Formatting);
        var text = new StringBuilder(capacity);
        Root.Render(text);
        return text.ToString();
    }

    /// <summary>The <c>include</c> directives of the file as read, at any depth: the files nginx loads with it.</summary>
    public IEnumerable<SyntaxDirective> Includes() => IncludesIn(Root);

    private static IEnumerable<SyntaxDirective> IncludesIn(SyntaxBlock block) => block.Lines
        .SelectMany(line => line.Items)
        .Select(item => item.Node)
        .OfType<SyntaxDirective>()
        .SelectMany(directive => directive.Name == Include && directive.Arguments.Count == 1
            ? [directive]
            : directive.Block is { } nested ? IncludesIn(nested) : []);
}
