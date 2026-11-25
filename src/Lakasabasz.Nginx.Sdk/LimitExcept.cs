using Lakasabasz.Nginx.Sdk.Internal.Structure;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>
/// The <c>limit_except</c> block of a location: what applies to requests with methods other than the listed ones, e.g.
/// <c>limit_except GET { deny all; }</c>. Its directives (<c>allow</c>, <c>deny</c>, <c>auth_basic</c>…) are in
/// <see cref="Section.Directives"/>.
/// </summary>
public sealed class LimitExcept : Section
{
    internal const string Keyword = "limit_except";

    /// <summary>A loose block for the methods other than <paramref name="methods"/>, e.g. <c>new LimitExcept("GET")</c>.</summary>
    public LimitExcept(params string[] methods)
        : base(Keyword, methods)
    {
    }

    internal LimitExcept(StructureDirective node)
        : base(node)
    {
    }

    /// <summary>The methods the block does not apply to, e.g. <c>GET</c> (which includes <c>HEAD</c>).</summary>
    public IReadOnlyList<string> Methods
    {
        get => Node.Arguments;
        set => Node.SetArguments([.. value]);
    }
}
