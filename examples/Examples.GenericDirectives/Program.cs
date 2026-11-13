// Generic directives: any directive is read and changed by its name and arguments. Only the changed lines are
// written anew, comments and formatting of the rest of the file stay as they were.
// Usage: Examples.GenericDirectives [site], by default sites-available/default of Debian/Ubuntu's nginx.
using Lakasabasz.Nginx.Sdk;

var path = args.Length > 0 ? args[0] : "sites-available/default";
var site = Nginx.Load(path).Config.All.Single();
var server = site.Directives.Single(d => d.Name == "server");

// Exactly one client_max_body_size: changes the existing one (and removes duplicates) or adds it
server.Directives.Set("client_max_body_size", "50m");

// A new directive goes after the last one, indented like it; arguments are quoted only when nginx needs it
server.Directives.Add("add_header", "X-Served-By", "nginx sdk example");

// A block, filled through its own directives
var api = server.Directives.AddBlock("location", "/api");
api.Directives.Add("proxy_pass", "http://127.0.0.1:5000");
api.Directives.Add("proxy_set_header", "Host", "$host");

// Removing takes the whole line, with a comment after the directive
foreach (var listen in server.Directives.Where(d => d.Name == "listen" && d.Arguments[0].StartsWith('[')).ToList())
    server.Directives.Remove(listen);

// Nothing is written to disk yet: saving, testing and reloading the changes comes with Apply/CommitChanges.
// Until then the file's new text is available through the temporary Serialize().
Console.WriteLine(site.Serialize());
return 0;
