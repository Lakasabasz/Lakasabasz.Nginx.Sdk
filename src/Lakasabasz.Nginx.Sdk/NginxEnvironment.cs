namespace Lakasabasz.Nginx.Sdk;

/// <summary>
/// Where the nginx installation lives and how its commands are run. Every value defaults to a standard
/// Debian/Ubuntu installation, so only what differs needs to be given, e.g.
/// <c>new NginxEnvironment(Binary: "/opt/nginx/sbin/nginx")</c>.
/// </summary>
/// <param name="Binary">nginx executable.</param>
/// <param name="ConfigRoot">Directory of the main config.</param>
/// <param name="ErrorLog">Error log.</param>
/// <param name="Sudo">Run nginx commands through <c>sudo -n</c>.</param>
public sealed record NginxEnvironment(
    string Binary = NginxEnvironment.DefaultBinary,
    string ConfigRoot = NginxEnvironment.DefaultConfigRoot,
    string ErrorLog = NginxEnvironment.DefaultErrorLog,
    bool Sudo = false)
{
    /// <summary><c>/usr/sbin/nginx</c></summary>
    public const string DefaultBinary = "/usr/sbin/nginx";

    /// <summary><c>/etc/nginx</c></summary>
    public const string DefaultConfigRoot = "/etc/nginx";

    /// <summary><c>/var/log/nginx/error.log</c></summary>
    public const string DefaultErrorLog = "/var/log/nginx/error.log";
}
