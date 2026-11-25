// Properties: sections (servers, locations…) have a typed property for each directive. Setting one writes its
// directive, null removes it; lists such as Names, Listens or Headers change their directives in place. Does the
// same as Examples.GenericDirectives and Examples.NginxText.
// Usage: Examples.Properties [site], by default sites-available/default of Debian/Ubuntu's nginx.
using Lakasabasz.Nginx.Sdk;

var path = args.Length > 0 ? args[0] : "sites-available/default";
var site = Nginx.Load(path).Config.All.Single();
var server = site.Servers.Single();
Console.WriteLine($"Server {string.Join(" ", server.Names)} on ports {string.Join(", ", server.Ports)}");

// One client_max_body_size: changes the existing one (and removes duplicates) or adds it
server.ClientMaxBodySize = Size.FromMegabytes(50);

// A response header (add_header)
server.Headers.Add(new Header("X-Served-By", "nginx sdk example"));

// A location, set up through its properties
var api = server.Locations.Add("/api");
api.ProxyPass = "http://127.0.0.1:5000";
api.Proxy.Headers["Host"] = "$host";

// Removing a listen removes its line
foreach (var listen in server.Listens.Where(listen => listen.Address?.StartsWith('[') == true).ToList())
    server.Listens.Remove(listen);

// Nothing is written to disk yet: saving, testing and reloading the changes comes with Apply/CommitChanges.
// Until then the file's new text is available through the temporary Serialize().
Console.WriteLine(site.Serialize());
return 0;
