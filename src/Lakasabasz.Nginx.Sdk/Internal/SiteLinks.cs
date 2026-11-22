namespace Lakasabasz.Nginx.Sdk.Internal;

/// <summary>
/// The links in <c>sites-enabled</c>: which sites of <c>sites-available</c> nginx loads. Knows only paths, never the
/// content of a site: the files are in <see cref="ConfigWorkspace"/>. A regular file in <c>sites-enabled</c> is not a
/// link, so it is not here; it is config like any other file.
/// </summary>
internal sealed class SiteLinks
{
    private readonly NginxPath _sitesAvailable;
    private readonly List<NginxPath> _targets = [];

    /// <summary>The links read from <c>sites-enabled</c> on disk.</summary>
    /// <param name="configRoot">nginx's config directory.</param>
    public SiteLinks(NginxPath configRoot)
    {
        _sitesAvailable = configRoot.Append(ConfigLayout.SitesAvailable).ResolveLinks();
        foreach (var entry in configRoot.Append(ConfigLayout.SitesEnabled).Entries().Where(entry => entry.IsLink))
            _targets.Add(entry.ResolveLinks());
    }

    /// <summary>The real paths the links point at, in the order of <c>sites-enabled</c>; new links come last.</summary>
    public IReadOnlyList<NginxPath> Targets => _targets;

    public bool IsLinked(NginxPath site) => _targets.Contains(site);

    /// <summary>Links the site; nothing happens when it is linked already.</summary>
    /// <exception cref="ArgumentException">The site is not directly in <c>sites-available</c>.</exception>
    public void Link(NginxPath site)
    {
        if (!site.IsIn(_sitesAvailable))
            throw new ArgumentException($"{site} is not in {ConfigLayout.SitesAvailable}: only sites there can be enabled.", nameof(site));
        if (!IsLinked(site))
            _targets.Add(site);
    }

    /// <summary>Removes the link to the site; nothing happens when there is none.</summary>
    public void Unlink(NginxPath site) => _targets.Remove(site);
}
