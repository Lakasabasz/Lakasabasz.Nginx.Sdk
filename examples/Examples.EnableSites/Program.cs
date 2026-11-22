// Enabling sites: a site of sites-available is enabled by a link in sites-enabled. SitesEnabled is only on/off;
// the content of a site is its file. Links are made and removed only when the changes are applied.
using Lakasabasz.Nginx.Sdk;

var nginx = Nginx.Discover();

foreach (var site in nginx.Config.SitesAvailable)
    Console.WriteLine($"{site.Path.Name}: {(nginx.SitesEnabled.Contains(site) ? "enabled" : "disabled")}");

// Enable a new site, and disable the default one
var shop = nginx.Config.SitesAvailable.Create("example-shop");
shop.Directives.AddBlock("server").Directives.Add("listen", "8080");
nginx.SitesEnabled.Enable(shop);

if (nginx.Config.SitesAvailable.TryGet("default", out var defaultSite))
    nginx.SitesEnabled.Disable(defaultSite);

Console.WriteLine($"Enabled now: {string.Join(", ", nginx.SitesEnabled.Select(site => site.Name))}");
return 0;
