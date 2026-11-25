using Lakasabasz.Nginx.Sdk.Internal.Structure;
using Lakasabasz.Nginx.Sdk.Views;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>A <c>server { }</c> block: a virtual server with its locations.</summary>
/// <example>
/// <code>
/// var server = site.Servers.Add();
/// server.Directives.Add("server_name", "example.com");
/// server.Locations.Add("/api").Directives.Add("proxy_pass", "http://127.0.0.1:5000");
/// </code>
/// </example>
public sealed class Server : Section
{
    internal const string Keyword = "server";

    /// <summary>A loose <c>server { }</c>, in no file until it is added to a <see cref="ServerCollection"/>.</summary>
    public Server()
        : base(Keyword)
    {
    }

    internal Server(StructureDirective node)
        : base(node)
    {
    }

    /// <summary>The <c>location</c> blocks of the server (not nested ones).</summary>
    public LocationCollection Locations => new(SectionScope.Of(Block));
}
