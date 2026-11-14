// Working with files: get, create and delete files of a directory by name. Create and the indexer are different
// verbs on purpose: there is no get-or-create. Changes stay in memory until they are applied.
using Lakasabasz.Nginx.Sdk;

var nginx = Nginx.Discover();

// An existing site: the indexer throws when it is not loaded, TryGet says so instead
if (nginx.Config.SitesAvailable.TryGet("default", out var defaultSite))
    Console.WriteLine($"default has {defaultSite.Directives.Count} top-level directives");

// A new site: Create throws when the file is loaded or exists on disk
const string name = "example-shop";
var shop = nginx.Config.SitesAvailable.Create(name);
var server = shop.Directives.AddBlock("server");
server.Directives.Add("listen", "8080");
server.Directives.Add("server_name", "shop.example.test");
Console.WriteLine($"Created {shop.Path}, {nginx.Config.All.Count} files loaded");

// Nothing is written yet: the new file's text is available through the temporary Serialize() until applying exists
Console.WriteLine(shop.Serialize());

// Deleting takes the file out of the loaded config (and disables it, if it was enabled)
nginx.Config.SitesAvailable.Delete(name);
Console.WriteLine($"Deleted {name}: still loaded? {nginx.Config.SitesAvailable.TryGet(name, out _)}");

return 0;
