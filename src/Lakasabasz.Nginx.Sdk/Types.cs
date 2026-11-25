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

    /// <summary>The extensions of each content type, e.g. <c>MimeTypes["text/html"] = ["html", "htm"]</c>.</summary>
    public IDictionary<string, IReadOnlyList<string>> MimeTypes => new DirectiveDictionary<IReadOnlyList<string>>(
        Block,
        _ => true,
        directive => directive.Name,
        directive => directive.Arguments,
        (type, extensions) => (type, extensions),
        StringComparer.OrdinalIgnoreCase);
}
