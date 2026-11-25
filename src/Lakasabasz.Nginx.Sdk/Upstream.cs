using Lakasabasz.Nginx.Sdk.Internal.Structure;
using Lakasabasz.Nginx.Sdk.Views;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>An <c>upstream name { }</c> block: a group of servers that <c>proxy_pass http://name</c> balances between.</summary>
public sealed class Upstream : Section
{
    internal const string Keyword = "upstream";

    /// <summary>A loose upstream, e.g. <c>new Upstream("backend")</c>, in no file until it is added to an <see cref="UpstreamCollection"/>.</summary>
    public Upstream(string name)
        : base(Keyword, name)
    {
    }

    internal Upstream(StructureDirective node)
        : base(node)
    {
    }

    /// <summary>The name used in <c>proxy_pass http://name</c>.</summary>
    public string Name
    {
        get => Node.Arguments.Count == 1 ? Node.Arguments[0] : throw DirectiveValues.Unreadable(Node, "one name");
        set => Node.SetArguments([value]);
    }
}
