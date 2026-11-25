// Style and comments: new and changed text is written in the style of the file (line break, indentation, braces,
// empty lines before a block), read from it; a block can have a style of its own. Text that is not changed stays
// byte for byte. Comments go to fixed places: at the end of a directive's line, right above a block, at the top of
// the file.
// Usage: Examples.StyleAndComments [site], by default sites-available/default of Debian/Ubuntu's nginx.
using Lakasabasz.Nginx.Sdk;

var path = args.Length > 0 ? args[0] : "sites-available/default";
var nginx = Nginx.Load(path);
var site = nginx.Config.All.Single();
var server = site.Servers.Single();

// The style of a file is read from it
var style = site.Style;
Console.WriteLine($"Style of {site.Path.Name}: {style.LineBreak}, {style.Indentation}, braces {style.Braces}, "
    + $"{style.BlockSpacing} empty line(s) before a block");

// A comment at the top of the file, and one at the end of an existing directive's line
site.Comment = "Changed by Examples.StyleAndComments";
server.Directives.First(directive => directive.Name == "listen").Comment = "IPv4";

// A location on one line, with its comment right above it
var health = server.Locations.Add(LocationModifier.Exact, "/health");
health.Style = health.Style with { Braces = BraceStyle.OneLine };
health.Comment = "Health check for the load balancer";
health.Directives.Add("return", "204");

// A location with its brace on a line of its own
var api = server.Locations.Add("/api");
api.Style = api.Style with { Braces = BraceStyle.NewLine };
api.ProxyPass = "http://127.0.0.1:5000";

// A new file gets the default style unless one is set: here tabs and two empty lines before each block
var shop = nginx.Config.SitesAvailable.Create("example-shop");
shop.Style = shop.Style with { Indentation = Indentation.Tabs(), BlockSpacing = 2 };
var shopServer = shop.Servers.Add();
shopServer.Names.Add("shop.example.test");
shopServer.Locations.Add("/").Root = "/var/www/shop";

// Nothing is written to disk yet: saving, testing and reloading the changes comes with Apply/CommitChanges.
// Until then the file's new text is available through the temporary Serialize().
Console.WriteLine(site.Serialize());
Console.WriteLine(shop.Serialize());
return 0;
