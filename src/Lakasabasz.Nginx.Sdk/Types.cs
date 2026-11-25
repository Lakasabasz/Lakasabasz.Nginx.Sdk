using Lakasabasz.Nginx.Sdk.Internal.Structure;
using Lakasabasz.Nginx.Sdk.Views;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>A <c>types { }</c> block: the content type of each file extension, e.g. <c>text/html html htm;</c>.</summary>
public sealed class Types : Section
{
    internal const string Keyword = "types";

    /// <summary>A loose, empty <c>types { }</c>, in no file until it is set as <see cref="HttpSection.Types"/>.</summary>
    public Types()
        : base(Keyword)
    {
    }

    internal Types(StructureDirective node)
        : base(node)
    {
    }

    /// <summary>
    /// The extensions of each content type, by the type (case-insensitive, as nginx compares them), e.g.
    /// <c>MimeTypes["text/html"] = ["html", "htm"]</c>.
    /// </summary>
    public IDictionary<string, IReadOnlyList<string>> MimeTypes =>
        new DirectiveDictionary<IReadOnlyList<string>>(Block, IsMimeType, ReadMimeType, WriteMimeType, StringComparer.OrdinalIgnoreCase);

    /// <summary>Every directive of a <c>types</c> block is a content type: its name is the type, its arguments the extensions.</summary>
    private static bool IsMimeType(StructureDirective directive) => true;

    private static KeyValuePair<string, IReadOnlyList<string>> ReadMimeType(StructureDirective directive) =>
        new(directive.Name, directive.Arguments);

    private static (string Name, IReadOnlyList<string> Arguments) WriteMimeType(string type, IReadOnlyList<string> extensions) =>
        (type, extensions);
}
