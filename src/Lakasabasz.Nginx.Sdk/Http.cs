using Lakasabasz.Nginx.Sdk.Internal.Structure;
using Lakasabasz.Nginx.Sdk.Views;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>The <c>http { }</c> block of <c>nginx.conf</c>: the HTTP servers, upstreams and their shared settings.</summary>
public sealed class Http : Section
{
    internal const string Keyword = "http";

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
}
