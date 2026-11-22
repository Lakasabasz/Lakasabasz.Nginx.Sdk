using Lakasabasz.Nginx.Sdk.Internal;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>
/// Entry point: an nginx installation (<see cref="Server"/>) and its loaded config files.
/// Create it with <see cref="Discover(bool)"/>, <see cref="Load"/> or <see cref="Init(bool)"/>.
/// </summary>
public sealed class Nginx
{
    private Nginx(NginxEnvironment environment, Action<ConfigWorkspace> load)
    {
        Server = new NginxServer(environment);
        var links = new SiteLinks(environment.ConfigRoot);
        var workspace = new ConfigWorkspace(environment.ConfigRoot, onDeleted: links.Unlink);
        load(workspace);
        Config = new NginxConfig(workspace);
        SitesEnabled = new NginxSitesEnabled(links);
    }

    /// <summary>The installation: environment, version, test and reload.</summary>
    public NginxServer Server { get; }

    /// <summary>The loaded config files.</summary>
    public NginxConfig Config { get; }

    /// <summary>Which sites of <c>sites-available</c> are enabled: only the links in <c>sites-enabled</c>, not the content.</summary>
    public NginxSitesEnabled SitesEnabled { get; }

    /// <summary>Default environment, no config files loaded.</summary>
    /// <param name="sudo">Run nginx commands through <c>sudo -n</c>.</param>
    public static Nginx Init(bool sudo = false) => Init(new NginxEnvironment(sudo: sudo));

    /// <summary>
    /// Finds and loads every config file of the installation: <c>nginx.conf</c> with everything it includes (what
    /// nginx loads), every site of <c>sites-available</c>, enabled or not, and the <c>*.conf</c> files of
    /// <c>conf.d</c> and <c>snippets</c>.
    /// </summary>
    /// <param name="sudo">Run nginx commands through <c>sudo -n</c>.</param>
    /// <exception cref="NginxParseException">A file has a syntax error or an included file does not exist; lists every broken file.</exception>
    public static Nginx Discover(bool sudo = false) => Discover(new NginxEnvironment(sudo: sudo));

    /// <summary>
    /// <see cref="Discover(bool)"/> for an installation outside the standard paths, e.g. with its own config root.
    /// </summary>
    /// <param name="environment">Paths of the installation and whether commands use sudo.</param>
    /// <exception cref="NginxParseException">A file has a syntax error or an included file does not exist; lists every broken file.</exception>
    public static Nginx Discover(NginxEnvironment environment) =>
        new(environment, workspace => workspace.Discover());

    /// <summary>Loads the given config file and, optionally, the files it includes.</summary>
    /// <param name="path">Config file to load; a relative path starts at the config root (<c>/etc/nginx</c>).</param>
    /// <param name="followIncludes">
    /// Also load the files named by <c>include</c> directives, at any depth (relative paths start at the config
    /// root, globs are expanded, each file is loaded once).
    /// </param>
    /// <param name="sudo">Run nginx commands through <c>sudo -n</c>.</param>
    /// <exception cref="NginxParseException">A file has a syntax error or an included file does not exist; lists every broken file.</exception>
    public static Nginx Load(NginxPath path, bool followIncludes = false, bool sudo = false)
    {
        var environment = new NginxEnvironment(sudo: sudo);
        return new Nginx(environment, workspace =>
        {
            if (followIncludes)
                workspace.LoadIncludeChain(path);
            else
                workspace.Load(path);
        });
    }

    /// <summary>
    /// <see cref="Init(bool)"/> for an installation outside the standard paths: own binary, config root and error log.
    /// </summary>
    /// <param name="environment">Paths of the installation and whether commands use sudo.</param>
    public static Nginx Init(NginxEnvironment environment) => new(environment, _ => { });
}
