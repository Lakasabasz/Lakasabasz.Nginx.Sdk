using Lakasabasz.Nginx.Sdk.Internal.Structure;
using Lakasabasz.Nginx.Sdk.Views;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>
/// The <c>proxy_*</c> directives of a block: how requests are passed to a proxied server. The address itself is
/// <see cref="Location.ProxyPass"/>.
/// </summary>
public sealed class ProxySettings
{
    private const string SetHeaderName = "proxy_set_header";
    private const string ConnectTimeoutName = "proxy_connect_timeout";
    private const string ReadTimeoutName = "proxy_read_timeout";
    private const string SendTimeoutName = "proxy_send_timeout";
    private const string HttpVersionName = "proxy_http_version";
    private const string BufferingName = "proxy_buffering";

    private readonly StructureBlock _block;

    internal ProxySettings(StructureBlock block)
    {
        _block = block;
    }

    /// <summary>
    /// The request headers passed to the proxied server (<c>proxy_set_header</c>), by name (case-insensitive), e.g.
    /// <c>Headers["Host"] = "$host"</c>.
    /// </summary>
    public IDictionary<string, string> Headers =>
        new DirectiveDictionary<string>(_block, IsSetHeader, ReadHeader, WriteHeader, StringComparer.OrdinalIgnoreCase);

    /// <summary>How long connecting to the proxied server may take (<c>proxy_connect_timeout</c>).</summary>
    public TimeSpan? ConnectTimeout
    {
        get => DirectiveValues.GetTime(_block, ConnectTimeoutName);
        set => DirectiveValues.SetTime(_block, ConnectTimeoutName, value);
    }

    /// <summary>How long the proxied server may take between two reads of its response (<c>proxy_read_timeout</c>).</summary>
    public TimeSpan? ReadTimeout
    {
        get => DirectiveValues.GetTime(_block, ReadTimeoutName);
        set => DirectiveValues.SetTime(_block, ReadTimeoutName, value);
    }

    /// <summary>How long the proxied server may take between two writes of the request (<c>proxy_send_timeout</c>).</summary>
    public TimeSpan? SendTimeout
    {
        get => DirectiveValues.GetTime(_block, SendTimeoutName);
        set => DirectiveValues.SetTime(_block, SendTimeoutName, value);
    }

    /// <summary>The HTTP version used with the proxied server, <c>1.0</c> or <c>1.1</c> (<c>proxy_http_version</c>).</summary>
    public string? HttpVersion
    {
        get => DirectiveValues.GetText(_block, HttpVersionName);
        set => DirectiveValues.SetText(_block, HttpVersionName, value);
    }

    /// <summary>Responses of the proxied server are buffered (<c>proxy_buffering on|off</c>).</summary>
    public bool? Buffering
    {
        get => DirectiveValues.GetSwitch(_block, BufferingName);
        set => DirectiveValues.SetSwitch(_block, BufferingName, value);
    }

    private static bool IsSetHeader(StructureDirective directive) => directive.Name == SetHeaderName;

    /// <summary><c>proxy_set_header Host $host;</c> as <c>Host</c> → <c>$host</c>.</summary>
    private static KeyValuePair<string, string> ReadHeader(StructureDirective directive) => directive.Arguments is [var name, var value]
        ? new KeyValuePair<string, string>(name, value)
        : throw DirectiveValues.NotA(directive, "a header and its value");

    private static (string Name, IReadOnlyList<string> Arguments) WriteHeader(string name, string value) => (SetHeaderName, [name, value]);
}
