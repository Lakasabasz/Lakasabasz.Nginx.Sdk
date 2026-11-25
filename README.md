# Lakasabasz.Nginx.Sdk

.NET library by Łukasz Mastalerz for reading, changing and writing nginx config files from C#, then testing
and reloading nginx: load → C# objects → modify → serialize → save → `nginx -t` → errors as exceptions →
`nginx -s reload`.

> The source of truth is [eu.buddy.works/lakasabasz/lakasabasz-nginx-sdk](https://eu.buddy.works/lakasabasz/lakasabasz-nginx-sdk).
> The GitHub repository is a read-only mirror, updated on every push; changes made there are overwritten.

> **Status: in development, not published yet.** The API below is the design the package is built towards;
> the [current state](#current-state) lists what already works.

## Principles

- **The package controls the content of the config files, not their effect on nginx.** It does not model
  directive inheritance or runtime behaviour.
- **A manager object** (like IIS `ServerManager` or certbot's `NginxConfigurator`), not a remote API client.
  No dependency injection required.
- **A file is always loaded whole.** Changes are tracked; saving serializes the whole file and overwrites it.
  Only changed files are written, and comments and formatting of unchanged parts are kept.
- **The root object maps the installation** (`Server`), the files in `/etc/nginx` (`Config`) and the enabled
  sites (`SitesEnabled`), not the directive structure. Directives appear inside a file.

## Usage

```csharp
var nginx = Nginx.Discover(sudo: true);   // finds every config file of the installation

nginx.Config.SitesAvailable.Create("reports")
    .AddServer(s => s
        .AddName("reports.example.com")
        .ListenOn(443, ssl: true, http2: true)
        .UseCertificate("/etc/ssl/fullchain.pem", "/etc/ssl/privkey.pem")
        .AddLocation("/api", l => l
            .ProxyTo("http://127.0.0.1:5000", p => p
                .ForwardHostHeader()
                .ForwardClientIpHeaders()))
        .AddLocation("/", l => l.ServeFrom("/var/www/reports")));
nginx.SitesEnabled.Enable(nginx.Config.SitesAvailable["reports"]);

nginx.CommitChanges();   // nginx -t on the changes, then save and reload; throws when nginx rejects them
```

Other entry points: `Nginx.Load(path, followIncludes)` loads chosen files, `Nginx.Init()` starts with none.
Sections can be filled with a fluent API (above), properties, lambdas, reusable objects (e.g. header or SSL
profiles), generic directives for anything outside the official nginx directive index, or plain nginx text.

## Current state

| Area | State |
|---|---|
| `Nginx.Init(sudo)`, `Nginx.Init(NginxEnvironment)` | works |
| `nginx.Server.Environment` | works |
| Lossless parser and serializer: comments and formatting of unchanged parts kept, Lua blocks kept as text | works |
| `Nginx.Load(path, followIncludes)`, `nginx.Config.All`; `NginxParseException` listing every broken file | works |
| Generic directives: `Directives.Add/Set/Remove/AddBlock` on files and blocks | works |
| `Nginx.Discover`; `nginx.Config`: `Main`, `Default`, `SitesAvailable`, `ConfigDirectory`, `Snippets`, path indexer, `Load`, `Find`; `Create`/`TryGet`/`Delete` of files; `nginx.SitesEnabled` (changes kept in memory until committing exists) | works |
| Sections (`Http`, `Events`, `Server`, `Location`, `Upstream`, `Types`, `LimitExcept`): made in code or read from a file, `Add`/`Remove`/LINQ on their collections | works |
| Properties for core HTTP, proxy, SSL and header directives (`null` removes); `Server.Names`, `Listens`, `Ports`, `SslPorts`, `IsDefault` | works |
| Plain nginx text: `AddFromText` on every section collection | works |
| Raw access to any directive (`Directive.Text`) | works |
| Style of new text (`NginxStyle` of a file or block) and comments by fixed rules | planned |
| Fluent API, lambdas, reusable objects; typed properties for the rest of the official directive index | planned |
| Committing changes (`GetChanges`, `Test`, `Apply`, `CommitChanges`); `nginx.Server`: `Version`, `Test()`, `Reload()` | planned |

```csharp
var config = Nginx.Discover(sudo: true);         // nginx.conf with its includes, every site, conf.d, snippets
var site = config.Config.SitesAvailable["reports"];
config.SitesEnabled.Enable(site);               // only the link in sites-enabled; the content is the file
var server = site.Servers.Single(s => s.Names.Contains("reports.example.com"));
server.ClientMaxBodySize = Size.FromMegabytes(50);          // only this line changes
server.Locations.Add("/api").ProxyPass = "http://127.0.0.1:5000";
server.Locations.AddFromText("/static/", "root /var/www/reports;\nexpires 7d;");
server.Directives.Set("more_set_headers", "Server: hidden"); // any directive, also of modules outside nginx
```

The same server can be changed with properties, generic directives and nginx text in any order: they all change
the same objects. The examples in `examples/` (`Examples.Properties`, `Examples.GenericDirectives`,
`Examples.NginxText`) do the same task in each style.

`NginxEnvironment` defaults to a standard Debian/Ubuntu installation (`/usr/sbin/nginx`, `/etc/nginx`); give only what differs, e.g. `new NginxEnvironment(binary: "/opt/nginx/sbin/nginx")`.

## Requirements

- Any .NET implementation supporting .NET Standard 2.0 (.NET 8/10, .NET Framework 4.6.2+, Mono).
- nginx on the same machine. Commands that need root run through `sudo -n`, so the user needs a passwordless
  sudo rule for the nginx binary.

## Development

The tests are end to end: they use the package's public API against a real nginx (1.28.3 from Ubuntu) and check
how it reacts. They run in a test image (`tests/environment`) as an unprivileged user with sudo rules:

```sh
tests/run-in-docker.sh                                              # all tests
tests/run-in-docker.sh --filter "FullyQualifiedName~NginxSudoTests" # some of them
```

Outside the image (e.g. in Rider) the tests that need nginx are ignored. Buddy runs the tests on every push
(pipeline *Tests*) and reports each result to a Buddy unit test suite.

## License

[MIT](LICENSE)
