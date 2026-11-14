// Finding the config: Discover loads nginx.conf with everything it includes (what nginx loads), every site of
// sites-available, enabled or not, and the *.conf files of conf.d and snippets. Nothing is read from disk later.
using Lakasabasz.Nginx.Sdk;

var nginx = Nginx.Discover();

Console.WriteLine($"Main config: {nginx.Config.Main.Path}");
Console.WriteLine($"Loaded files: {nginx.Config.All.Count}");
foreach (var file in nginx.Config.All)
    Console.WriteLine($"  {file.Path}");

// The layout of /etc/nginx: every directory is a collection of files, so LINQ works on it
Console.WriteLine($"Sites: {string.Join(", ", nginx.Config.SitesAvailable.Select(site => Path.GetFileName(site.Path)))}");
Console.WriteLine($"Enabled: {string.Join(", ", nginx.SitesEnabled.Select(site => Path.GetFileName(site.Path)))}");
Console.WriteLine($"conf.d: {string.Join(", ", nginx.Config.ConfigDirectory.Select(file => Path.GetFileName(file.Path)))}");

// Find searches the loaded files by name or by a glob in the path relative to /etc/nginx
foreach (var file in nginx.Config.Find("*.conf"))
    Console.WriteLine($"*.conf: {file.Path}");

// Any loaded file by path; an enabled site's link gives its file in sites-available
if (nginx.SitesEnabled.FirstOrDefault() is { } enabled)
{
    var link = $"sites-enabled/{Path.GetFileName(enabled.Path)}";
    Console.WriteLine($"{link} is {nginx.Config[link].Path}");
}

return 0;
