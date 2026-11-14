using Lakasabasz.Nginx.Sdk.Internal;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>
/// The loaded config files of the installation, mapped to the layout of its config root (<c>/etc/nginx</c>): the main
/// file, the directories of sites, configs and snippets, and every file by path.
/// </summary>
/// <remarks>
/// Only loaded files are here (see <see cref="All"/>): nothing is read from disk on access. Changes (new and deleted
/// files) stay in this object until they are applied.
/// </remarks>
public sealed class NginxConfig
{
    private const string DefaultSite = "default";

    private readonly ConfigWorkspace _workspace;

    internal NginxConfig(ConfigWorkspace workspace)
    {
        _workspace = workspace;
        SitesAvailable = new NginxDirectory(workspace, ConfigWorkspace.SitesAvailable);
        ConfigDirectory = new NginxDirectory(workspace, ConfigWorkspace.ConfigDirectory);
        Snippets = new NginxDirectory(workspace, ConfigWorkspace.Snippets);
    }

    /// <summary>The main config file, <c>nginx.conf</c> in the config root.</summary>
    /// <exception cref="KeyNotFoundException">The main file is not loaded.</exception>
    public NginxFile Main => this[ConfigWorkspace.MainFile];

    /// <summary>The default site, <c>sites-available/default</c> (Debian and Ubuntu).</summary>
    /// <exception cref="KeyNotFoundException">The default site is not loaded, e.g. in a layout without <c>sites-available</c>.</exception>
    public NginxFile Default => SitesAvailable[DefaultSite];

    /// <summary>The sites, <c>sites-available/*</c>, enabled or not; which are enabled is <see cref="Nginx.SitesEnabled"/>.</summary>
    public NginxDirectory SitesAvailable { get; }

    /// <summary>The configs of the <c>http</c> block, <c>conf.d/*</c>.</summary>
    public NginxDirectory ConfigDirectory { get; }

    /// <summary>Fragments included by other files, <c>snippets/*</c>.</summary>
    public NginxDirectory Snippets { get; }

    /// <summary>A loaded file by path, e.g. <c>Config["sites-enabled/00-default"]</c>.</summary>
    /// <param name="path">
    /// Relative to the config root, or absolute. Links are resolved: an enabled site's link gives its file in
    /// <c>sites-available</c>.
    /// </param>
    /// <exception cref="KeyNotFoundException">The file is not loaded; load it with <see cref="Load"/>.</exception>
    public NginxFile this[string path] => _workspace.Get(_workspace.Resolve(path))
        ?? throw new KeyNotFoundException($"{path} is not loaded: load it with Config.Load.");

    /// <summary>
    /// Every loaded file: after <c>Nginx.Discover</c> everything found, after <c>Nginx.Load</c> the given file (and
    /// what it includes), after <c>Nginx.Init</c> none. Files loaded with <see cref="Load"/> or created later are
    /// added at the end; deleted ones are removed.
    /// </summary>
    public IReadOnlyList<NginxFile> All => _workspace.Files;

    /// <summary>Loads one existing file (not the files it includes) and adds it to <see cref="All"/>.</summary>
    /// <param name="path">Relative to the config root, or absolute.</param>
    /// <returns>The file; the same object when it is loaded already.</returns>
    /// <exception cref="NginxParseException">The file does not exist or has a syntax error.</exception>
    public NginxFile Load(string path) => _workspace.Load(path);

    /// <summary>The loaded files matching a file name or a glob in their path.</summary>
    /// <param name="pattern">
    /// Without <c>/</c> it matches the file name (<c>"upstreams.conf"</c>, <c>"*.conf"</c>); with <c>/</c> the path
    /// relative to the config root (<c>"conf.d/*"</c>), or the full path when it starts with <c>/</c>. <c>*</c> and
    /// <c>?</c> do not cross <c>/</c>. Enabled sites have the path of their file in <c>sites-available</c>.
    /// </param>
    /// <returns>The matching files in <see cref="All"/> order; empty when none match. Only loaded files are searched.</returns>
    public IReadOnlyList<NginxFile> Find(string pattern) => _workspace.Find(pattern);
}
