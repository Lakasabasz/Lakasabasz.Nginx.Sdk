// Layout and comments: new and changed text is written in the file's line breaks and indentation, and each block
// keeps or gets its own layout (on one line, brace on a line of its own). Text that is not changed stays byte for
// byte. Comments are added on lines of their own or at the end of a directive's line.
// Usage: Examples.LayoutAndComments [site], by default sites-available/default of Debian/Ubuntu's nginx.
using Lakasabasz.Nginx.Sdk;

var path = args.Length > 0 ? args[0] : "sites-available/default";
var nginx = Nginx.Load(path);
var site = nginx.Config.All.Single();
var server = site.Servers.Single();

// The layout of a file is read from it: new lines are indented like their neighbours
Console.WriteLine($"Indentation: {(site.Layout.Indentation == "\t" ? "tabs" : $"{site.Layout.Indentation.Length} spaces")}");

// A comment on a line of its own, then a location on one line
server.Directives.AddComment("Health check for the load balancer");
var health = server.Locations.Add(LocationModifier.Exact, "/health");
health.Layout = new BlockLayout { OneLine = true };
health.Directives.Add("return", "204");

// A location with its brace on a line of its own and a comment after it
var api = server.Locations.Add("/api");
api.Layout = new BlockLayout { BraceOnNewLine = true };
api.ProxyPass = "http://127.0.0.1:5000";
api.Comment = "the backend";

// A comment at the end of an existing directive's line
server.Directives.First(directive => directive.Name == "listen").Comment = "IPv4";

// A new file gets the default layout (\n, four spaces) unless one is set
var shop = nginx.Config.SitesAvailable.Create("example-shop");
shop.Layout = new NginxLayout { Indentation = "\t" };
shop.Servers.Add().Locations.Add("/").Root = "/var/www/shop";

// Nothing is written to disk yet: saving, testing and reloading the changes comes with Apply/CommitChanges.
// Until then the file's new text is available through the temporary Serialize().
Console.WriteLine(site.Serialize());
Console.WriteLine(shop.Serialize());
return 0;
