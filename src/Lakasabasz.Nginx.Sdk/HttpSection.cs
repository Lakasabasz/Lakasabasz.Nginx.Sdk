using Lakasabasz.Nginx.Sdk.Internal.Structure;
using Lakasabasz.Nginx.Sdk.Views;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>
/// What <see cref="Http"/>, <see cref="Server"/> and <see cref="Location"/> have in common: the directives nginx allows
/// in all three (core HTTP, proxy and response headers).
/// </summary>
public abstract class HttpSection : Section
{
    private const string RootName = "root";
    private const string IndexName = "index";
    private const string IncludeName = "include";
    private const string ClientMaxBodySizeName = "client_max_body_size";
    private const string ServerTokensName = "server_tokens";
    private const string KeepAliveTimeoutName = "keepalive_timeout";
    private const string DefaultTypeName = "default_type";
    private const string SendFileName = "sendfile";
    private const string GzipName = "gzip";
    private const string AddHeaderName = "add_header";

    private protected HttpSection(StructureDirective node)
        : base(node)
    {
    }

    private protected HttpSection(string name, params string[] arguments)
        : base(name, arguments)
    {
    }

    /// <summary>The directory requests are served from (<c>root</c>).</summary>
    public string? Root
    {
        get => DirectiveValues.GetText(Block, RootName);
        set => DirectiveValues.SetText(Block, RootName, value);
    }

    /// <summary>The files tried for a request ending with <c>/</c> (<c>index</c>), from every <c>index</c> directive.</summary>
    public IList<string> Index => new ArgumentList(Block, IndexName);

    /// <summary>The files included here (<c>include</c>), one per directive, e.g. <c>snippets/ssl.conf</c>; their content is not followed.</summary>
    public IList<string> Includes => new DirectiveList<string>(Block, IncludeName, ReadInclude, path => [path]);

    /// <summary>The largest request body (<c>client_max_body_size</c>); 0 means no limit.</summary>
    public Size? ClientMaxBodySize
    {
        get => DirectiveValues.GetSize(Block, ClientMaxBodySizeName);
        set => DirectiveValues.SetSize(Block, ClientMaxBodySizeName, value);
    }

    /// <summary>The nginx version in error pages and the <c>Server</c> header (<c>server_tokens on|off</c>).</summary>
    public bool? ServerTokens
    {
        get => DirectiveValues.GetSwitch(Block, ServerTokensName);
        set => DirectiveValues.SetSwitch(Block, ServerTokensName, value);
    }

    /// <summary>How long an idle keep-alive connection stays open (<c>keepalive_timeout</c>).</summary>
    public TimeSpan? KeepAliveTimeout
    {
        get => DirectiveValues.GetTime(Block, KeepAliveTimeoutName);
        set => DirectiveValues.SetTime(Block, KeepAliveTimeoutName, value);
    }

    /// <summary>The content type of a response whose extension has no type (<c>default_type</c>).</summary>
    public string? DefaultType
    {
        get => DirectiveValues.GetText(Block, DefaultTypeName);
        set => DirectiveValues.SetText(Block, DefaultTypeName, value);
    }

    /// <summary>Files are sent by the kernel (<c>sendfile on|off</c>).</summary>
    public bool? SendFile
    {
        get => DirectiveValues.GetSwitch(Block, SendFileName);
        set => DirectiveValues.SetSwitch(Block, SendFileName, value);
    }

    /// <summary>Responses are compressed (<c>gzip on|off</c>).</summary>
    public bool? Gzip
    {
        get => DirectiveValues.GetSwitch(Block, GzipName);
        set => DirectiveValues.SetSwitch(Block, GzipName, value);
    }

    /// <summary>The response headers added here (<c>add_header</c>), one per directive.</summary>
    public IList<Header> Headers => new DirectiveList<Header>(Block, AddHeaderName, Header.Read, header => header.Arguments());

    /// <summary>How requests are passed to a proxied server (<c>proxy_*</c>).</summary>
    public ProxySettings Proxy => new(Block);

    /// <summary>The <c>types { }</c> block mapping extensions to content types; <see langword="null"/> removes it.</summary>
    public Types? Types
    {
        get => Sections.Get(Block, Types.Keyword, directive => new Types(directive));
        set => Sections.Set(Block, Types.Keyword, value);
    }

    private static string ReadInclude(StructureDirective directive) => directive.Arguments.Count == 1
        ? directive.Arguments[0]
        : throw DirectiveValues.NotA(directive, "one file");
}
