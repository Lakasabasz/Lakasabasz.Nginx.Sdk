namespace Lakasabasz.Nginx.Sdk;

/// <summary>
/// Where the nginx installation lives and how its commands are run. Every value defaults to a standard
/// Debian/Ubuntu installation, so only what differs needs to be given, e.g.
/// <c>new NginxEnvironment(binary: "/opt/nginx/sbin/nginx")</c>.
/// </summary>
public sealed record NginxEnvironment
{
    /// <summary>The environment; every path not given is the standard one.</summary>
    /// <param name="binary">nginx executable; <see cref="DefaultBinary"/> when not given.</param>
    /// <param name="configRoot">Directory of the main config; <see cref="DefaultConfigRoot"/> when not given.</param>
    /// <param name="sudo">Run nginx commands through <c>sudo -n</c>.</param>
    public NginxEnvironment(NginxPath? binary = null, NginxPath? configRoot = null, bool sudo = false)
    {
        Binary = binary ?? DefaultBinary;
        ConfigRoot = configRoot ?? DefaultConfigRoot;
        Sudo = sudo;
    }

    /// <summary><c>/usr/sbin/nginx</c></summary>
    public static NginxPath DefaultBinary { get; } = "/usr/sbin/nginx";

    /// <summary><c>/etc/nginx</c></summary>
    public static NginxPath DefaultConfigRoot { get; } = "/etc/nginx";

    /// <summary>nginx executable.</summary>
    public NginxPath Binary { get; init; }

    /// <summary>Directory of the main config.</summary>
    public NginxPath ConfigRoot { get; init; }

    /// <summary>Run nginx commands through <c>sudo -n</c>.</summary>
    public bool Sudo { get; init; }
}
