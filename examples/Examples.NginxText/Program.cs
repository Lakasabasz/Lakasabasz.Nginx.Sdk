// nginx text: a ready fragment of config is pasted as a new section with AddFromText. The section is then an object
// like any other, so the rest of the task uses properties. Does the same as Examples.GenericDirectives and
// Examples.Properties.
// Usage: Examples.NginxText [site], by default sites-available/default of Debian/Ubuntu's nginx.
using Lakasabasz.Nginx.Sdk;

var path = args.Length > 0 ? args[0] : "sites-available/default";
var site = Nginx.Load(path).Config.All.Single();
var server = site.Servers.Single();

// The location as nginx text: its body, without "location /api {" and "}"
var api = server.Locations.AddFromText("/api", """
    proxy_pass http://127.0.0.1:5000;
    proxy_set_header Host $host;
    """);
Console.WriteLine($"Pasted location {api.Path} passes to {api.ProxyPass}");

// Text adds sections; the existing directives are changed through properties
server.ClientMaxBodySize = Size.FromMegabytes(50);
server.Headers.Add(new Header("X-Served-By", "nginx sdk example"));
foreach (var listen in server.Listens.Where(listen => listen.Address?.StartsWith('[') == true).ToList())
    server.Listens.Remove(listen);

// A syntax error in the text is reported at its line and column in the text
try
{
    server.Locations.AddFromText("/broken", "proxy_pass http://127.0.0.1:5000");
}
catch (NginxParseException exception)
{
    Console.WriteLine($"Not added: {exception.Errors[0].Message} at {exception.Errors[0].Line}:{exception.Errors[0].Column}");
}

// Nothing is written to disk yet: saving, testing and reloading the changes comes with Apply/CommitChanges.
// Until then the file's new text is available through the temporary Serialize().
Console.WriteLine(site.Serialize());
return 0;
