using System.Text;
using Lakasabasz.Nginx.Sdk.Internal.Structure;

namespace Lakasabasz.Nginx.Sdk.Internal.Syntax;

/// <summary>
/// The syntax tree of one file: its lines as written, the formatting new text gets, and, through
/// <see cref="SyntaxBinding"/>, the internal structure read from it. The file side gives it text and takes text back,
/// without seeing the structure; the facades reach the structure through it. Rendering first writes the changes of the
/// structure into the lines.
/// </summary>
internal sealed class SyntaxTree
{
    private const string Include = "include";

    /// <summary>Where a syntax error in a piece of nginx text is reported: the text is in no file.</summary>
    private static readonly NginxPath FragmentPath = new("(nginx text)");

    private readonly SyntaxBinding _binding;

    /// <summary>The tree of a file's text, its style read from it.</summary>
    public SyntaxTree(SyntaxBlock root, string text)
    {
        Root = root;
        _binding = SyntaxBinding.Bind(root, StyleDetection.Of(text, root));
    }

    public SyntaxBlock Root { get; }

    /// <summary>The internal structure of the file: its directives, without formatting.</summary>
    public StructureBlock Structure => _binding.Structure;

    /// <summary>
    /// The directives of a piece of nginx text, e.g. the body of a section pasted in code; a syntax error
    /// is at a line and column of the text.
    /// </summary>
    public static Result<List<StructureDirective>, NginxParseError> Fragment(string text) => ConfigParser.Parse(FragmentPath, text)
        .Map(tree => SyntaxBinding.Fragment(tree.Root));

    /// <summary>The file's text: the changes of the structure applied, everything unchanged as it was read.</summary>
    public string Render(int capacity)
    {
        _binding.Apply(Root);
        var text = new StringBuilder(capacity);
        Root.Render(text);
        return text.ToString();
    }

    /// <summary>
    /// The <c>include</c> directives of the file as read, at any depth: the files nginx loads with it. Only an include
    /// with one argument names a file; any other is a syntax nginx rejects, and is left out.
    /// </summary>
    public IEnumerable<SyntaxDirective> Includes() => IncludesIn(Root);

    private static IEnumerable<SyntaxDirective> IncludesIn(SyntaxBlock block) => block.Lines
        .SelectMany(line => line.Items)
        .Select(item => item.Node)
        .OfType<SyntaxDirective>()
        .SelectMany(directive => directive.Name == Include && directive.Arguments.Count == 1
            ? [directive]
            : directive.Block is { } nested ? IncludesIn(nested) : []);
}
