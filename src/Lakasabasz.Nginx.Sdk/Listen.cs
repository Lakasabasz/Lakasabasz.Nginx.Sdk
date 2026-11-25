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
    private const string Ipv6OnlyOn = "ipv6only=on";
    private const string Ipv6OnlyOff = "ipv6only=off";
    private const string Ipv6Start = "[";
    private const string Ipv6End = "]";
    private const string PortSeparator = ":";
    private const string UnixPrefix = "unix:";
    private const int DefaultPort = 80;

    /// <summary>The flags of <c>listen</c>, in the order they are written.</summary>
    private static readonly (string Text, Func<Listen, bool> IsSet, Func<Listen, Listen> Set)[] Flags =
    [
        (SslFlag, listen => listen.Ssl, listen => listen with { Ssl = true }),
        (Http2Flag, listen => listen.Http2, listen => listen with { Http2 = true }),
        (QuicFlag, listen => listen.Quic, listen => listen with { Quic = true }),
        (ProxyProtocolFlag, listen => listen.ProxyProtocol, listen => listen with { ProxyProtocol = true }),
        (DefaultServerFlag, listen => listen.DefaultServer, listen => listen with { DefaultServer = true }),
        (ReusePortFlag, listen => listen.ReusePort, listen => listen with { ReusePort = true }),
    ];

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

    /// <summary>A unix socket (<c>unix:…</c>) rather than an address and port.</summary>
    public bool IsUnixSocket => Address?.StartsWith(UnixPrefix, StringComparison.Ordinal) == true;

    /// <summary>The port nginx listens on: <see cref="Port"/>, or 80 when none is given; <see langword="null"/> for a unix socket.</summary>
    public int? EffectivePort => IsUnixSocket ? null : Port ?? DefaultPort;

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
            throw Views.DirectiveValues.Unreadable(directive, "an address or port");

        var listen = AddressOf(arguments[0]);
        var parameters = new List<string>();
        foreach (var argument in arguments.Skip(1))
        {
            if (WithFlag(listen, argument) is { } flagged)
                listen = flagged;
            else
                parameters.Add(argument);
        }
        return listen with { Parameters = parameters };
    }

    /// <summary>The arguments of the <c>listen</c> directive: the address, the flags set (in <see cref="Flags"/> order), the rest.</summary>
    internal IReadOnlyList<string> ToArguments()
    {
        var arguments = new List<string> { AddressText() };
        arguments.AddRange(Flags.Where(flag => flag.IsSet(this)).Select(flag => flag.Text));
        if (Ipv6Only is { } ipv6Only)
            arguments.Add(ipv6Only ? Ipv6OnlyOn : Ipv6OnlyOff);
        arguments.AddRange(Parameters);
        return arguments;
    }

    /// <summary>The listen with one more known flag set; <see langword="null"/> for an argument that is not one.</summary>
    private static Listen? WithFlag(Listen listen, string argument) => argument switch
    {
        LegacyDefaultFlag => listen with { DefaultServer = true },
        Ipv6OnlyOn => listen with { Ipv6Only = true },
        Ipv6OnlyOff => listen with { Ipv6Only = false },
        _ => Flags.Where(flag => flag.Text == argument).Select(flag => flag.Set(listen)).FirstOrDefault(),
    };

    /// <summary>The address and port of <c>listen</c>: <c>80</c>, <c>127.0.0.1:8080</c>, <c>[::]:443</c>, <c>localhost</c>, <c>unix:…</c>.</summary>
    private static Listen AddressOf(string text)
    {
        if (text.StartsWith(UnixPrefix, StringComparison.Ordinal))
            return new Listen { Address = text };
        if (ParsePort(text) is { } port)
            return new Listen { Port = port };
        var (address, addressPort) = text.StartsWith(Ipv6Start, StringComparison.Ordinal) ? SplitIpv6(text) : SplitHost(text);
        return new Listen { Address = address, Port = addressPort };
    }

    /// <summary><c>[::1]:8080</c> → <c>[::1]</c> and 8080; <c>[::1]</c> → no port.</summary>
    private static (string Address, int? Port) SplitIpv6(string text)
    {
        var end = text.IndexOf(Ipv6End, StringComparison.Ordinal) + 1;
        var address = end > 0 ? text.Substring(0, end) : text;
        var rest = text.Substring(address.Length);
        return (address, rest.StartsWith(PortSeparator, StringComparison.Ordinal) ? ParsePort(rest.Substring(1)) : null);
    }

    /// <summary><c>127.0.0.1:8080</c> → <c>127.0.0.1</c> and 8080; <c>localhost</c> → no port.</summary>
    private static (string Address, int? Port) SplitHost(string text)
    {
        var separator = text.LastIndexOf(PortSeparator, StringComparison.Ordinal);
        if (separator < 0 || ParsePort(text.Substring(separator + 1)) is not { } port)
            return (text, null);
        return (text.Substring(0, separator), port);
    }

    private static int? ParsePort(string text) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var port) ? port : null;

    private string AddressText() => (Address, Port) switch
    {
        (null, null) => Number(DefaultPort),
        (null, { } port) => Number(port),
        ({ } address, null) => address,
        ({ } address, { } port) => address + PortSeparator + Number(port),
    };

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
