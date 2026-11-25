using Lakasabasz.Nginx.Sdk.Internal.Structure;
using Lakasabasz.Nginx.Sdk.Views;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>The <c>events { }</c> block of <c>nginx.conf</c>: how worker processes handle connections.</summary>
public sealed class Events : Section
{
    internal const string Keyword = "events";

    private const string WorkerConnectionsName = "worker_connections";
    private const string MultiAcceptName = "multi_accept";
    private const string UseName = "use";

    /// <summary>A loose <c>events { }</c>, in no file until it is set as <see cref="NginxFile.Events"/>.</summary>
    public Events()
        : base(Keyword)
    {
    }

    internal Events(StructureDirective node)
        : base(node)
    {
    }

    /// <summary>The most connections a worker process has open at once (<c>worker_connections</c>).</summary>
    public int? WorkerConnections
    {
        get => DirectiveValues.Get(Block, WorkerConnectionsName, DirectiveValues.Number);
        set => DirectiveValues.Set(Block, WorkerConnectionsName, DirectiveValues.Number, value);
    }

    /// <summary>A worker accepts all new connections at once (<c>multi_accept on|off</c>).</summary>
    public bool? MultiAccept
    {
        get => DirectiveValues.Get(Block, MultiAcceptName, DirectiveValues.Switch);
        set => DirectiveValues.Set(Block, MultiAcceptName, DirectiveValues.Switch, value);
    }

    /// <summary>The connection processing method, e.g. <c>epoll</c> (<c>use</c>); nginx picks the best one without it.</summary>
    public string? Use
    {
        get => DirectiveValues.GetText(Block, UseName);
        set => DirectiveValues.SetText(Block, UseName, value);
    }
}
