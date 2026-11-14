using System.Collections;
using Lakasabasz.Nginx.Sdk.Internal;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>
/// Which sites are enabled: a site of <c>sites-available</c> is enabled by a link to it in <c>sites-enabled</c>. This
/// is on or off only; the content of a site is its file in <c>Config.SitesAvailable</c>.
/// </summary>
/// <remarks>
/// Read from <c>sites-enabled</c> when the <see cref="Nginx"/> is created. Changes stay in this object; links are made
/// and removed only when the changes are applied.
/// </remarks>
public sealed class NginxSitesEnabled : IEnumerable<NginxFile>
{
    private readonly ConfigWorkspace _workspace;

    internal NginxSitesEnabled(ConfigWorkspace workspace)
    {
        _workspace = workspace;
    }

    /// <summary>Enables a site; nothing happens when it is enabled already.</summary>
    /// <param name="site">A loaded file of <c>sites-available</c>.</param>
    /// <exception cref="ArgumentException">The file is not a loaded site of <c>sites-available</c>.</exception>
    public void Add(NginxFile site) => _workspace.Enable(site);

    /// <summary>Disables a site; nothing happens when it is not enabled.</summary>
    /// <param name="site">The site.</param>
    /// <exception cref="InvalidOperationException">
    /// The site is a regular file in <c>sites-enabled</c>, not a link: there is nothing to unlink.
    /// </exception>
    public void Remove(NginxFile site) => _workspace.Disable(site);

    /// <summary>Whether the site is enabled.</summary>
    /// <param name="site">The site.</param>
    public bool Contains(NginxFile site) => _workspace.IsEnabled(site);

    /// <summary>The enabled sites that are loaded, in <c>Config.All</c> order.</summary>
    public IEnumerator<NginxFile> GetEnumerator() => _workspace.EnabledSites.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
