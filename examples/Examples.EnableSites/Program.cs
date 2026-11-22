// Enabling sites: a site of sites-available is enabled by a link in sites-enabled. SitesEnabled manages only these
// links; the content of a site is its file. Changes are made in memory and written by CommitChanges.
using Lakasabasz.Nginx.Sdk;

var nginx = Nginx.Discover(sudo: true);

foreach (var site in nginx.Config.SitesAvailable)
    Console.WriteLine($"{site.Path.Name}: {(nginx.SitesEnabled.Contains(site) ? "enabled" : "disabled")}");

// Enable a new site, and disable the default one
var shop = nginx.Config.SitesAvailable.Create("example-shop");
shop.Directives.AddBlock("server").Directives.Add("listen", "8080");
nginx.SitesEnabled.Enable(shop);

if (nginx.Config.SitesAvailable.TryGet("default", out var defaultSite))
    nginx.SitesEnabled.Disable(defaultSite);

// nginx -t on the changes, then the file and the links are written and nginx is reloaded
nginx.CommitChanges();
Console.WriteLine($"Enabled: {string.Join(", ", nginx.SitesEnabled.Select(site => site.Name))}");
return 0;
