// Enabling sites: a site of sites-available is enabled by a link in sites-enabled. SitesEnabled is only on/off;
// the content of a site is its file. Links are made and removed only when the changes are applied.
using Lakasabasz.Nginx.Sdk;

var nginx = Nginx.Discover();

foreach (var site in nginx.Config.SitesAvailable)
    Console.WriteLine($"{Path.GetFileName(site.Path)}: {(nginx.SitesEnabled.Contains(site) ? "enabled" : "disabled")}");

// Enable a new site, and disable the default one
var shop = nginx.Config.SitesAvailable.Create("example-shop");
shop.Directives.AddBlock("server").Directives.Add("listen", "8080");
nginx.SitesEnabled.Add(shop);

if (nginx.Config.SitesAvailable.TryGet("default", out var defaultSite))
    nginx.SitesEnabled.Remove(defaultSite);

Console.WriteLine($"Enabled now: {string.Join(", ", nginx.SitesEnabled.Select(site => Path.GetFileName(site.Path)))}");
return 0;
