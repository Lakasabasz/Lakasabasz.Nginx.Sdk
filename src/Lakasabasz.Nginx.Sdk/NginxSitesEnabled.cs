using System.Collections;
using Lakasabasz.Nginx.Sdk.Internal;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>
/// Which sites are enabled: a site of <c>sites-available</c> is enabled by a link to it in <c>sites-enabled</c>. Only
/// the links: the content of a site is its file in <c>Config.SitesAvailable</c>, and nothing is loaded or added to
/// the config here.
/// </summary>
/// <remarks>
/// Read from <c>sites-enabled</c> when the <see cref="Nginx"/> is created. A regular file there is not a link, so it
/// is not listed here; it is config, reached through <c>Config["sites-enabled/…"]</c>. Changes stay in this object;
/// links are made and removed only when the changes are applied.
/// </remarks>
public sealed class NginxSitesEnabled : IEnumerable<NginxPath>
{
    private readonly SiteLinks _links;

    internal NginxSitesEnabled(SiteLinks links)
    {
        _links = links;
    }

    /// <summary>Enables a site: a link to it in <c>sites-enabled</c>. Nothing happens when it is enabled already.</summary>
    /// <param name="site">A site of <c>sites-available</c>; only its <see cref="NginxFile.Path"/> is used.</param>
    /// <exception cref="ArgumentException">The file is not in <c>sites-available</c>.</exception>
    public void Enable(NginxFile site) => _links.Link(site.Path);

    /// <summary>Disables a site: its link goes. Nothing happens when it is not enabled.</summary>
    /// <param name="site">The site; only its <see cref="NginxFile.Path"/> is used.</param>
    public void Disable(NginxFile site) => _links.Unlink(site.Path);

    /// <summary>Whether the site is enabled: there is a link to its path.</summary>
    /// <param name="site">The site; only its <see cref="NginxFile.Path"/> is used.</param>
    public bool Contains(NginxFile site) => _links.IsLinked(site.Path);

    /// <summary>The paths of the enabled sites (the targets of the links), loaded or not.</summary>
    public IEnumerator<NginxPath> GetEnumerator() => _links.Targets.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
