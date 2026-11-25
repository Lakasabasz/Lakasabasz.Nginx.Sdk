using Lakasabasz.Nginx.Sdk.Internal.Structure;
using Lakasabasz.Nginx.Sdk.Views;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>An <c>upstream name { }</c> block: a group of servers that <c>proxy_pass http://name</c> balances between.</summary>
public sealed class Upstream : Section
{
    internal const string Keyword = "upstream";

    private const string ServerName = "server";
    private const string KeepAliveName = "keepalive";
    private const string KeepAliveTimeoutName = "keepalive_timeout";

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
        get => Node.Arguments.Count == 1 ? Node.Arguments[0] : throw DirectiveValues.NotA(Node, "one name");
        set => Node.SetArguments([value]);
    }

    /// <summary>The servers of the group, one per <c>server</c> directive.</summary>
    public IList<UpstreamServer> Servers => new DirectiveList<UpstreamServer>(Block, ServerName, UpstreamServer.Read, server => server.Arguments());

    /// <summary>The idle connections to the servers each worker keeps open (<c>keepalive</c>).</summary>
    public int? KeepAlive
    {
        get => DirectiveValues.GetNumber(Block, KeepAliveName);
        set => DirectiveValues.SetNumber(Block, KeepAliveName, value);
    }

    /// <summary>How long an idle connection to a server stays open (<c>keepalive_timeout</c>).</summary>
    public TimeSpan? KeepAliveTimeout
    {
        get => DirectiveValues.GetTime(Block, KeepAliveTimeoutName);
        set => DirectiveValues.SetTime(Block, KeepAliveTimeoutName, value);
    }
}
