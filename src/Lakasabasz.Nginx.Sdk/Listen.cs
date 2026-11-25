using System.Globalization;
using Lakasabasz.Nginx.Sdk.Internal.Structure;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>
/// One <c>listen</c> directive of a server, e.g. <c>listen [::]:443 ssl default_server;</c>. Immutable: to change a
/// listen of a server, set a new value in <see cref="Server.Listens"/>, e.g. <c>server.Listens[0] = server.Listens[0] with { Port = 8443 }</c>.
/// </summary>
public sealed record Listen
{
    private const string DefaultServerFlag = "default_server";
    private const string LegacyDefaultFlag = "default";
    private const string SslFlag = "ssl";
    private const string Http2Flag = "http2";
    private const string QuicFlag = "quic";
    private const string ProxyProtocolFlag = "proxy_protocol";
    private const string ReusePortFlag = "reuseport";
    private const string Ipv6OnlyParameter = "ipv6only=";
    private const string UnixPrefix = "unix:";

    /// <summary>The address without the port, e.g. <c>127.0.0.1</c>, <c>[::]</c>, <c>*</c> or <c>unix:/run/site.sock</c>; <see langword="null"/> for all addresses.</summary>
    public string? Address { get; init; }

    /// <summary>The port; <see langword="null"/> when the directive gives none (nginx then listens on 80).</summary>
    public int? Port { get; init; }

    /// <summary>The default server of this address and port (<c>default_server</c>).</summary>
    public bool DefaultServer { get; init; }

    /// <summary>Connections are TLS (<c>ssl</c>).</summary>
    public bool Ssl { get; init; }

    /// <summary>HTTP/2 on this listen (<c>http2</c>; newer nginx prefers the <c>http2</c> directive).</summary>
    public bool Http2 { get; init; }

    /// <summary>HTTP/3 over QUIC (<c>quic</c>).</summary>
    public bool Quic { get; init; }

    /// <summary>Connections start with the PROXY protocol header (<c>proxy_protocol</c>).</summary>
    public bool ProxyProtocol { get; init; }

    /// <summary>A socket per worker process (<c>reuseport</c>).</summary>
    public bool ReusePort { get; init; }

    /// <summary>An IPv6 address takes only IPv6 connections (<c>ipv6only=on</c>) or both (<c>off</c>); <see langword="null"/> when not given.</summary>
    public bool? Ipv6Only { get; init; }

    /// <summary>The other parameters, as written, e.g. <c>backlog=512</c>.</summary>
    public IReadOnlyList<string> Parameters { get; init; } = [];

    /// <inheritdoc />
    public bool Equals(Listen? other) => other is not null
        && Address == other.Address && Port == other.Port && DefaultServer == other.DefaultServer && Ssl == other.Ssl
        && Http2 == other.Http2 && Quic == other.Quic && ProxyProtocol == other.ProxyProtocol && ReusePort == other.ReusePort
        && Ipv6Only == other.Ipv6Only && Parameters.SequenceEqual(other.Parameters);

    /// <inheritdoc />
    public override int GetHashCode() => (Address, Port, DefaultServer, Ssl).GetHashCode();

    internal static Listen Read(StructureDirective directive)
    {
        var arguments = directive.Arguments;
        if (arguments.Count == 0)
            throw Views.DirectiveValues.NotA(directive, "an address or port");

        var listen = AddressOf(arguments[0]);
        var parameters = new List<string>();
        foreach (var argument in arguments.Skip(1))
        {
            listen = argument switch
            {
                DefaultServerFlag or LegacyDefaultFlag => listen with { DefaultServer = true },
                SslFlag => listen with { Ssl = true },
                Http2Flag => listen with { Http2 = true },
                QuicFlag => listen with { Quic = true },
                ProxyProtocolFlag => listen with { ProxyProtocol = true },
                ReusePortFlag => listen with { ReusePort = true },
                Ipv6OnlyParameter + "on" => listen with { Ipv6Only = true },
                Ipv6OnlyParameter + "off" => listen with { Ipv6Only = false },
                _ => listen,
            };
            if (!IsKnown(argument))
                parameters.Add(argument);
        }
        return listen with { Parameters = parameters };
    }

    internal IReadOnlyList<string> Arguments()
    {
        var arguments = new List<string> { AddressText() };
        AddIf(arguments, Ssl, SslFlag);
        AddIf(arguments, Http2, Http2Flag);
        AddIf(arguments, Quic, QuicFlag);
        AddIf(arguments, ProxyProtocol, ProxyProtocolFlag);
        AddIf(arguments, DefaultServer, DefaultServerFlag);
        AddIf(arguments, ReusePort, ReusePortFlag);
        if (Ipv6Only is { } ipv6Only)
            arguments.Add(Ipv6OnlyParameter + (ipv6Only ? "on" : "off"));
        arguments.AddRange(Parameters);
        return arguments;
    }

    /// <summary>The address and port of <c>listen</c>: <c>80</c>, <c>127.0.0.1:8080</c>, <c>[::]:443</c>, <c>localhost</c>, <c>unix:…</c>.</summary>
    private static Listen AddressOf(string text)
    {
        if (text.StartsWith(UnixPrefix, StringComparison.Ordinal))
            return new Listen { Address = text };
        if (ParsePort(text) is { } port)
            return new Listen { Port = port };

        var portStart = text.StartsWith("[", StringComparison.Ordinal) ? text.IndexOf("]:", StringComparison.Ordinal) + 1 : text.LastIndexOf(':');
        if (portStart > 0 && ParsePort(text.Substring(portStart + 1)) is { } addressPort)
            return new Listen { Address = text.Substring(0, portStart), Port = addressPort };
        return new Listen { Address = text };
    }

    private static int? ParsePort(string text) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var port) ? port : null;

    private string AddressText() => (Address, Port) switch
    {
        (null, null) => "80",
        (null, { } port) => port.ToString(CultureInfo.InvariantCulture),
        ({ } address, null) => address,
        ({ } address, { } port) => address + ":" + port.ToString(CultureInfo.InvariantCulture),
    };

    private static bool IsKnown(string argument) => argument is DefaultServerFlag or LegacyDefaultFlag or SslFlag or Http2Flag
        or QuicFlag or ProxyProtocolFlag or ReusePortFlag or Ipv6OnlyParameter + "on" or Ipv6OnlyParameter + "off";

    private static void AddIf(List<string> arguments, bool flag, string text)
    {
        if (flag)
            arguments.Add(text);
    }
}
