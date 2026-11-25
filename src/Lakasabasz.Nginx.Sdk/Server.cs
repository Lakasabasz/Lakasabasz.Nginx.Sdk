using Lakasabasz.Nginx.Sdk.Internal.Structure;
using Lakasabasz.Nginx.Sdk.Views;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>A <c>server { }</c> block: a virtual server with its names, listens and locations.</summary>
/// <example>
/// <code>
/// var server = site.Servers.Add();
/// server.Names.Add("example.com");
/// server.Listens.Add(new Listen { Port = 443, Ssl = true });
/// server.Ssl.Certificate = "/etc/ssl/fullchain.pem";
/// server.Locations.Add("/api").ProxyPass = "http://127.0.0.1:5000";
/// </code>
/// </example>
public sealed class Server : HttpSection
{
    internal const string Keyword = "server";

    private const string NameName = "server_name";
    private const string ListenName = "listen";
    private const string TryFilesName = "try_files";

    /// <summary>A loose <c>server { }</c>, in no file until it is added to a <see cref="ServerCollection"/>.</summary>
    public Server()
        : base(Keyword)
    {
    }

    internal Server(StructureDirective node)
        : base(node)
    {
    }

    /// <summary>
    /// The names of the server (<c>server_name</c>), from every <c>server_name</c> directive, as written: exact names,
    /// wildcards (<c>*.example.com</c>) and regexes (<c>~^www\.</c>). <c>Contains</c> compares them as text.
    /// </summary>
    public IList<string> Names => new ArgumentList(Block, NameName);

    /// <summary>The addresses and ports the server listens on, one per <c>listen</c> directive.</summary>
    public IList<Listen> Listens => new DirectiveList<Listen>(Block, ListenName, Listen.Read, listen => listen.ToArguments());

    /// <summary>The ports of <see cref="Listens"/> (80 for a listen without a port; unix sockets have none).</summary>
    public IReadOnlyList<int> Ports => PortsOf(Listens);

    /// <summary>The ports of the TLS listens (<c>ssl</c>).</summary>
    public IReadOnlyList<int> SslPorts => PortsOf(Listens.Where(listen => listen.Ssl));

    /// <summary>Whether a listen makes this the default server of its address and port (<c>default_server</c>).</summary>
    public bool IsDefault => Listens.Any(listen => listen.DefaultServer);

    /// <summary>The <c>location</c> blocks of the server (not nested ones).</summary>
    public LocationCollection Locations => new(SectionScope.Of(Block));

    /// <summary>The certificate and TLS settings (<c>ssl_*</c>).</summary>
    public SslSettings Ssl => new(Block);

    /// <summary>The files tried in order, the last one being the fallback, e.g. <c>$uri $uri/ =404</c> (<c>try_files</c>).</summary>
    public IList<string> TryFiles => new ArgumentList(Block, TryFilesName);

    private static List<int> PortsOf(IEnumerable<Listen> listens) => listens
        .Where(listen => listen.EffectivePort is not null)
        .Select(listen => listen.EffectivePort!.Value)
        .Distinct()
        .ToList();
}
