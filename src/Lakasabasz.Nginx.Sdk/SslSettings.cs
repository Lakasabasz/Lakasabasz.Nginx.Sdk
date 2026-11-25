using Lakasabasz.Nginx.Sdk.Internal.Structure;
using Lakasabasz.Nginx.Sdk.Views;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>The <c>ssl_*</c> directives of <c>http</c> or of a server: the certificate and the TLS settings.</summary>
public sealed class SslSettings
{
    private const string CertificateName = "ssl_certificate";
    private const string CertificateKeyName = "ssl_certificate_key";
    private const string CiphersName = "ssl_ciphers";
    private const string PreferServerCiphersName = "ssl_prefer_server_ciphers";
    private const string SessionCacheName = "ssl_session_cache";
    private const string SessionTimeoutName = "ssl_session_timeout";

    private readonly StructureBlock _block;

    internal SslSettings(StructureBlock block)
    {
        _block = block;
    }

    /// <summary>The certificate file, in PEM, e.g. <c>/etc/ssl/fullchain.pem</c> (<c>ssl_certificate</c>).</summary>
    public string? Certificate
    {
        get => DirectiveValues.GetText(_block, CertificateName);
        set => DirectiveValues.SetText(_block, CertificateName, value);
    }

    /// <summary>The secret key of the certificate, e.g. <c>/etc/ssl/privkey.pem</c> (<c>ssl_certificate_key</c>).</summary>
    public string? CertificateKey
    {
        get => DirectiveValues.GetText(_block, CertificateKeyName);
        set => DirectiveValues.SetText(_block, CertificateKeyName, value);
    }

    /// <summary>The ciphers, in OpenSSL's format, e.g. <c>HIGH:!aNULL:!MD5</c> (<c>ssl_ciphers</c>).</summary>
    public string? Ciphers
    {
        get => DirectiveValues.GetText(_block, CiphersName);
        set => DirectiveValues.SetText(_block, CiphersName, value);
    }

    /// <summary>The server's order of ciphers wins over the client's (<c>ssl_prefer_server_ciphers on|off</c>).</summary>
    public bool? PreferServerCiphers
    {
        get => DirectiveValues.Get(_block, PreferServerCiphersName, DirectiveValues.Switch);
        set => DirectiveValues.Set(_block, PreferServerCiphersName, DirectiveValues.Switch, value);
    }

    /// <summary>The cache of TLS sessions, e.g. <c>shared:SSL:10m</c> (<c>ssl_session_cache</c>).</summary>
    public string? SessionCache
    {
        get => DirectiveValues.GetText(_block, SessionCacheName);
        set => DirectiveValues.SetText(_block, SessionCacheName, value);
    }

    /// <summary>How long a TLS session can be reused (<c>ssl_session_timeout</c>).</summary>
    public TimeSpan? SessionTimeout
    {
        get => DirectiveValues.Get(_block, SessionTimeoutName, DirectiveValues.Time);
        set => DirectiveValues.Set(_block, SessionTimeoutName, DirectiveValues.Time, value);
    }
}
