// Working with files: get, create and delete files of a directory by name. Create and the indexer are different
// verbs on purpose: there is no get-or-create. Changes are made in memory and written by CommitChanges.
using Lakasabasz.Nginx.Sdk;

var nginx = Nginx.Discover(sudo: true);

// An existing site: the indexer throws when it is not loaded, TryGet says so instead
if (nginx.Config.SitesAvailable.TryGet("default", out var defaultSite))
    Console.WriteLine($"default has {defaultSite.Directives.Count} top-level directives");

// A new site: Create throws when the file is loaded or exists on disk
const string name = "example-shop";
var shop = nginx.Config.SitesAvailable.Create(name);
var server = shop.Directives.AddBlock("server");
server.Directives.Add("listen", "8080");
server.Directives.Add("server_name", "shop.example.test");
nginx.CommitChanges();
Console.WriteLine($"Created {shop.Path}, {nginx.Config.All.Count} files loaded");

// Deleting takes the file out of the loaded config (and disables it, if it was enabled)
nginx.Config.SitesAvailable.Delete(name);
nginx.CommitChanges();
Console.WriteLine($"Deleted {name}: still loaded? {nginx.Config.SitesAvailable.TryGet(name, out _)}");

return 0;
