using Lakasabasz.Nginx.Sdk.Internal;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>
/// Entry point: an nginx installation (<see cref="Server"/>) and its loaded config files.
/// Create it with <see cref="Discover"/>, <see cref="Load"/> or <see cref="Init(bool)"/>.
/// </summary>
public sealed class Nginx
{
    private Nginx(NginxEnvironment environment, IReadOnlyList<NginxFile> files)
    {
        Server = new NginxServer(environment);
        Config = new NginxConfig(files);
    }

    /// <summary>The installation: environment, version, test and reload.</summary>
    public NginxServer Server { get; }

    /// <summary>The loaded config files.</summary>
    public NginxConfig Config { get; }

    /// <summary>Default environment, no config files loaded.</summary>
    /// <param name="sudo">Run nginx commands through <c>sudo -n</c>.</param>
    public static Nginx Init(bool sudo = false) => Init(new NginxEnvironment(Sudo: sudo));

    /// <summary>Finds and loads every config file of the installation.</summary>
    /// <param name="sudo">Run nginx commands through <c>sudo -n</c>.</param>
    public static Nginx Discover(bool sudo = false) =>
        throw new NotImplementedException("Etap 2: Wejścia i model plików");

    /// <summary>Loads the given config file and, optionally, the files it includes.</summary>
    /// <param name="path">Config file to load; a relative path starts at the config root (<c>/etc/nginx</c>).</param>
    /// <param name="followIncludes">
    /// Also load the files named by <c>include</c> directives, at any depth (relative paths start at the config
    /// root, globs are expanded, each file is loaded once).
    /// </param>
    /// <param name="sudo">Run nginx commands through <c>sudo -n</c>.</param>
    /// <exception cref="NginxParseException">A file has a syntax error or an included file does not exist; lists every broken file.</exception>
    public static Nginx Load(string path, bool followIncludes = false, bool sudo = false)
    {
        var environment = new NginxEnvironment(Sudo: sudo);
        return new Nginx(environment, ConfigLoader.Load(path, environment.ConfigRoot, followIncludes));
    }

    /// <summary>
    /// <see cref="Init(bool)"/> for an installation outside the standard paths: own binary, config root and error log.
    /// </summary>
    /// <param name="environment">Paths of the installation and whether commands use sudo.</param>
    public static Nginx Init(NginxEnvironment environment) => new(environment, []);
}
