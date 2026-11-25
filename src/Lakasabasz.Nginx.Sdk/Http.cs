using Lakasabasz.Nginx.Sdk.Internal.Structure;
using Lakasabasz.Nginx.Sdk.Views;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>The <c>http { }</c> block of <c>nginx.conf</c>: the HTTP servers, upstreams and their shared settings.</summary>
public sealed class Http : HttpSection
{
    internal const string Keyword = "http";

    private const string TcpNoPushName = "tcp_nopush";

    /// <summary>A loose <c>http { }</c>, in no file until it is set as <see cref="NginxFile.Http"/>.</summary>
    public Http()
        : base(Keyword)
    {
    }

    internal Http(StructureDirective node)
        : base(node)
    {
    }

    /// <summary>The <c>server</c> blocks written in this block (not those of included files).</summary>
    public ServerCollection Servers => new(SectionScope.Of(Block));

    /// <summary>The <c>upstream</c> blocks written in this block (not those of included files).</summary>
    public UpstreamCollection Upstreams => new(SectionScope.Of(Block));

    /// <summary>The certificate and TLS settings shared by the servers (<c>ssl_*</c>).</summary>
    public SslSettings Ssl => new(Block);

    /// <summary>Response headers are sent in one packet with the file's start (<c>tcp_nopush on|off</c>, with <c>sendfile</c>).</summary>
    public bool? TcpNoPush
    {
        get => DirectiveValues.GetSwitch(Block, TcpNoPushName);
        set => DirectiveValues.SetSwitch(Block, TcpNoPushName, value);
    }
}
