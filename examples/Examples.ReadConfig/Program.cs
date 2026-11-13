// Loading config files: the main file with everything it includes, then reading directives at any depth.
using Lakasabasz.Nginx.Sdk;

Nginx nginx;
try
{
    // A relative path starts at the config root (/etc/nginx); includes are followed at any depth, globs expanded
    nginx = Nginx.Load("nginx.conf", followIncludes: true);
}
catch (NginxParseException e)
{
    // Every file is parsed before this is thrown: one error for each broken file
    foreach (var error in e.Errors)
        Console.WriteLine($"{error.File}:{error.Line}:{error.Column}: {error.Message}");
    return 1;
}

foreach (var file in nginx.Config.All)
    Console.WriteLine($"{file.Path}: {file.Directives.Count} top-level directives");

// Every server block of every loaded file, with where it listens and the names it answers to
foreach (var file in nginx.Config.All)
{
    foreach (var server in Descendants(file.Directives).Where(d => d.Name == "server" && d.IsBlock))
    {
        var names = server.Directives.Where(d => d.Name == "server_name").SelectMany(d => d.Arguments);
        var listens = server.Directives.Where(d => d.Name == "listen").Select(d => string.Join(" ", d.Arguments));
        Console.WriteLine($"server at {file.Path}:{server.Line}");
        Console.WriteLine($"  server_name {string.Join(" ", names)}");
        Console.WriteLine($"  listen      {string.Join(", ", listens)}");
    }
}

return 0;

// The directives of a collection and of every block inside it
static IEnumerable<Directive> Descendants(DirectiveCollection directives) =>
    directives.SelectMany(d => Descendants(d.Directives).Prepend(d));
