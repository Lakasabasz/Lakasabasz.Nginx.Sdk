namespace Lakasabasz.Nginx.Sdk.Tests.Sections;

using Server = Lakasabasz.Nginx.Sdk.Server;

[NginxRequired(Mode.ReadOnly)]
public sealed class PropertyTests : TestTemplate
{
    private NginxFile _file;

    #region SetUp

    [SetUp]
    public void LoadProperties()
    {
        Arrange.Certificate();
        _file = NginxFile.Load(Arrange.PropertiesConfig());
    }

    #endregion

    #region Helpers

    private Http Http => _file.Http!;

    private Server Server => Http.Servers.Single();

    private Location Api => Server.Locations.First();

    private Location LocationOf(Directive location) => Server.Locations.Single(l => l.Directive == location);

    private static NginxFile Build()
    {
        var file = Nginx.Init().Config.SitesAvailable.Create(Consts.Sections.SiteName);
        file.Events = new Events
        {
            WorkerConnections = Consts.Sections.WorkerConnections,
            MultiAccept = true,
            Use = Consts.Sections.EventMethod,
        };
        var http = new Http
        {
            SendFile = true,
            TcpNoPush = true,
            KeepAliveTimeout = Consts.Sections.KeepAliveTimeout,
            ClientMaxBodySize = Consts.Sections.BodySize,
            ServerTokens = false,
            DefaultType = Consts.Sections.DefaultType,
            Gzip = true,
            Root = Consts.Sections.HtmlRoot,
            Index = { Consts.Sections.IndexHtml, Consts.Sections.IndexHtm },
            Includes = { Consts.Sections.ProxyHeadersSnippet },
            Headers = { new Header(Consts.Sections.FrameOptions, Consts.Sections.Deny) { Always = true } },
        };
        file.Http = http;
        http.Proxy.Headers[Consts.Sections.HostHeader] = Consts.Sections.HostVariable;
        http.Proxy.ConnectTimeout = Consts.Sections.ConnectTimeout;
        http.Proxy.ReadTimeout = Consts.Sections.ReadTimeout;
        http.Proxy.SendTimeout = Consts.Sections.SendTimeout;
        http.Proxy.HttpVersion = Consts.Sections.HttpVersion;
        http.Proxy.Buffering = false;
        http.Types = new Types { MimeTypes = { [Consts.Sections.HtmlType] = Consts.Sections.HtmlExtensions } };

        var upstream = http.Upstreams.Add(Consts.Sections.UpstreamName);
        upstream.Servers.Add(Consts.Sections.PrimaryBackend);
        upstream.Servers.Add(Consts.Sections.BackupBackend);
        upstream.KeepAlive = Consts.Sections.UpstreamKeepAlive;
        upstream.KeepAliveTimeout = Consts.Sections.UpstreamKeepAliveTimeout;

        var server = http.Servers.Add();
        server.Names.Add(Consts.Sections.ExampleName);
        server.Names.Add(Consts.Sections.WwwName);
        server.Listens.Add(Consts.Sections.SslListen);
        server.Listens.Add(Consts.Sections.Ipv6SslListen);
        foreach (var tryFile in Consts.Sections.TryFiles)
            server.TryFiles.Add(tryFile);
        server.Ssl.Certificate = Consts.Sections.Certificate;
        server.Ssl.CertificateKey = Consts.Sections.CertificateKey;
        server.Ssl.Ciphers = Consts.Sections.Ciphers;
        server.Ssl.PreferServerCiphers = true;
        server.Ssl.SessionCache = Consts.Sections.SessionCache;
        server.Ssl.SessionTimeout = Consts.Sections.SessionTimeout;

        var api = server.Locations.Add(Consts.Sections.ApiPath);
        api.ProxyPass = Consts.Sections.UpstreamUrl;
        api.LimitExcept = new LimitExcept(Consts.Sections.Get);
        api.LimitExcept.Directives.Add(Consts.Sections.Deny, Consts.Sections.All);
        server.Locations.Add(LocationModifier.PreferPrefix, Consts.Sections.StaticPath).Alias = Consts.Sections.StaticRoot;
        return file;
    }

    #endregion

    [Test]
    [Description("Every typed property written on new sections gives the expected file, which nginx accepts.")]
    public async Task WritesProperties()
    {
        var file = Build();

        var result = await NginxControl.TestAsync(file, WorkDirectory);

        Assert.Multiple(() =>
        {
            Assert.That(file.Serialize(), Is.EqualTo(Expected.Properties), "Each property should write its directive");
            Assert.That(result.Success, Is.True, Consts.Messages.NginxRejected(result));
        });
    }

    [Test]
    [Description("The properties of events read worker_connections, multi_accept and use from the file.")]
    public void ReadsEvents()
    {
        var events = _file.Events!;

        Assert.Multiple(() =>
        {
            Assert.That(events.WorkerConnections, Is.EqualTo(Consts.Sections.WorkerConnections), "worker_connections should be read");
            Assert.That(events.MultiAccept, Is.True, "multi_accept should be read");
            Assert.That(events.Use, Is.EqualTo(Consts.Sections.EventMethod), "use should be read");
        });
    }

    [Test]
    [Description("The properties shared by http, server and location read their directives from the file: switches, sizes, times, lists, headers.")]
    public void ReadsHttp()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Http.SendFile, Is.True, "sendfile should be read");
            Assert.That(Http.TcpNoPush, Is.True, "tcp_nopush should be read");
            Assert.That(Http.KeepAliveTimeout, Is.EqualTo(Consts.Sections.KeepAliveTimeout), "keepalive_timeout should be read as a time");
            Assert.That(Http.ClientMaxBodySize, Is.EqualTo(Consts.Sections.BodySize), "client_max_body_size should be read as a size");
            Assert.That(Http.ServerTokens, Is.False, "server_tokens should be read");
            Assert.That(Http.DefaultType, Is.EqualTo(Consts.Sections.DefaultType), "default_type should be read");
            Assert.That(Http.Gzip, Is.True, "gzip should be read");
            Assert.That(Http.Root, Is.EqualTo(Consts.Sections.HtmlRoot), "root should be read");
            Assert.That(Http.Index, Is.EqualTo(Consts.Sections.Indexes), "index should be read");
            Assert.That(Http.Includes, Is.EqualTo(Consts.Sections.Includes), "include should be read");
            Assert.That(Http.Headers.Single(), Is.EqualTo(new Header(Consts.Sections.FrameOptions, Consts.Sections.Deny) { Always = true }), "add_header should be read");
        });
    }

    [Test]
    [Description("The properties of a server read its names, listens and try_files from the file.")]
    public void ReadsServer()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Server.Names, Is.EqualTo(Consts.Sections.Names), "server_name should be read");
            Assert.That(Server.Listens, Is.EqualTo(Consts.Sections.Listens), "listen should be read");
            Assert.That(Server.TryFiles, Is.EqualTo(Consts.Sections.TryFiles), "try_files should be read");
        });
    }

    [Test]
    [Description("The ssl_* properties read the certificate and TLS settings from the file.")]
    public void ReadsSsl()
    {
        var ssl = Server.Ssl;

        Assert.Multiple(() =>
        {
            Assert.That(ssl.Certificate, Is.EqualTo(Consts.Sections.Certificate), "ssl_certificate should be read");
            Assert.That(ssl.CertificateKey, Is.EqualTo(Consts.Sections.CertificateKey), "ssl_certificate_key should be read");
            Assert.That(ssl.Ciphers, Is.EqualTo(Consts.Sections.Ciphers), "ssl_ciphers should be read");
            Assert.That(ssl.PreferServerCiphers, Is.True, "ssl_prefer_server_ciphers should be read");
            Assert.That(ssl.SessionCache, Is.EqualTo(Consts.Sections.SessionCache), "ssl_session_cache should be read");
            Assert.That(ssl.SessionTimeout, Is.EqualTo(Consts.Sections.SessionTimeout), "ssl_session_timeout should be read");
        });
    }

    [Test]
    [Description("The proxy_* properties read the passed headers, timeouts, HTTP version and buffering from the file.")]
    public void ReadsProxy()
    {
        var proxy = Http.Proxy;

        Assert.Multiple(() =>
        {
            Assert.That(proxy.Headers[Consts.Sections.HostHeaderLowerCase], Is.EqualTo(Consts.Sections.HostVariable), "proxy_set_header should be read by its name in any case");
            Assert.That(proxy.ConnectTimeout, Is.EqualTo(Consts.Sections.ConnectTimeout), "proxy_connect_timeout should be read");
            Assert.That(proxy.ReadTimeout, Is.EqualTo(Consts.Sections.ReadTimeout), "proxy_read_timeout should be read");
            Assert.That(proxy.SendTimeout, Is.EqualTo(Consts.Sections.SendTimeout), "proxy_send_timeout should be read");
            Assert.That(proxy.HttpVersion, Is.EqualTo(Consts.Sections.HttpVersion), "proxy_http_version should be read");
            Assert.That(proxy.Buffering, Is.False, "proxy_buffering should be read");
        });
    }

    [Test]
    [Description("The properties of locations read their path, modifier, proxy_pass, alias and limit_except from the file.")]
    public void ReadsLocations()
    {
        var staticFiles = Server.Locations.Last();

        Assert.Multiple(() =>
        {
            Assert.That(Api.Path, Is.EqualTo(Consts.Sections.ApiPath), "The path should be read");
            Assert.That(Api.Modifier, Is.EqualTo(LocationModifier.None), "A prefix location should have no modifier");
            Assert.That(Api.ProxyPass, Is.EqualTo(Consts.Sections.UpstreamUrl), "proxy_pass should be read");
            Assert.That(Api.LimitExcept?.Methods, Is.EqualTo(Consts.Sections.LimitedMethods), "limit_except should be read");
            Assert.That(staticFiles.Modifier, Is.EqualTo(LocationModifier.PreferPrefix), "The ^~ modifier should be read");
            Assert.That(staticFiles.Path, Is.EqualTo(Consts.Sections.StaticPath), "The path after the modifier should be read");
            Assert.That(staticFiles.Alias, Is.EqualTo(Consts.Sections.StaticRoot), "alias should be read");
        });
    }

    [Test]
    [Description("The properties of an upstream and of types read their name, servers, keepalive and content types from the file.")]
    public void ReadsUpstreamAndTypes()
    {
        var upstream = Http.Upstreams.Single();

        Assert.Multiple(() =>
        {
            Assert.That(upstream.Name, Is.EqualTo(Consts.Sections.UpstreamName), "The upstream's name should be read");
            Assert.That(upstream.Servers, Is.EqualTo(Consts.Sections.Backends), "The upstream's servers should be read");
            Assert.That(upstream.KeepAlive, Is.EqualTo(Consts.Sections.UpstreamKeepAlive), "keepalive should be read");
            Assert.That(upstream.KeepAliveTimeout, Is.EqualTo(Consts.Sections.UpstreamKeepAliveTimeout), "keepalive_timeout should be read");
            Assert.That(Http.Types?.MimeTypes[Consts.Sections.HtmlType], Is.EqualTo(Consts.Sections.HtmlExtensions), "types should be read");
        });
    }

    [Test]
    [Description("The keys of sections are changed by their properties: a location's path and modifier, an upstream's name, the methods of limit_except; nginx accepts the result.")]
    public async Task KeysChange()
    {
        Api.Path = Consts.Sections.RenamedPath;
        Server.Locations.Last().Modifier = LocationModifier.None;
        Http.Upstreams.Single().Name = Consts.Sections.RenamedUpstream;
        Api.ProxyPass = Consts.Sections.RenamedUpstreamUrl;
        Api.LimitExcept!.Methods = Consts.Sections.RenamedMethods;

        var result = await NginxControl.TestAsync(_file, WorkDirectory);

        Assert.Multiple(() =>
        {
            Assert.That(Api.Path, Is.EqualTo(Consts.Sections.RenamedPath), "The new path should be read");
            Assert.That(Server.Locations.Last().Path, Is.EqualTo(Consts.Sections.StaticPath), "Changing the modifier should keep the path");
            Assert.That(_file.Serialize(), Is.EqualTo(Expected.PropertiesRenamed), "Only the keys should change");
            Assert.That(result.Success, Is.True, Consts.Messages.NginxRejected(result));
        });
    }

    [Test]
    [Description("A modifier written together with the path, e.g. location =/exact or location ~\\.php$, is read apart from it.")]
    public void ReadsGluedModifiers()
    {
        var exact = LocationOf(Server.Directives.AddBlock(Consts.Sections.LocationBlock, Consts.Sections.GluedExact));
        var regex = LocationOf(Server.Directives.AddBlock(Consts.Sections.LocationBlock, Consts.Sections.GluedRegex));

        Assert.Multiple(() =>
        {
            Assert.That((exact.Modifier, exact.Path), Is.EqualTo((LocationModifier.Exact, Consts.Sections.ExactPath)), "=/exact should be an exact location");
            Assert.That((regex.Modifier, regex.Path), Is.EqualTo((LocationModifier.Regex, Consts.Sections.RegexPath)), "A glued ~ should be a regex location");
        });
    }

    [Test]
    [Description("Setting every property to null, or clearing its list, removes its directives and keeps the blocks.")]
    public void NullRemoves()
    {
        var events = _file.Events!;
        events.WorkerConnections = null;
        events.MultiAccept = null;
        events.Use = null;
        Http.SendFile = null;
        Http.TcpNoPush = null;
        Http.KeepAliveTimeout = null;
        Http.ClientMaxBodySize = null;
        Http.ServerTokens = null;
        Http.DefaultType = null;
        Http.Gzip = null;
        Http.Root = null;
        Http.Index.Clear();
        Http.Includes.Clear();
        Http.Headers.Clear();
        Http.Proxy.Headers.Clear();
        Http.Proxy.ConnectTimeout = null;
        Http.Proxy.ReadTimeout = null;
        Http.Proxy.SendTimeout = null;
        Http.Proxy.HttpVersion = null;
        Http.Proxy.Buffering = null;
        Http.Types = null;
        var upstream = Http.Upstreams.Single();
        upstream.Servers.Clear();
        upstream.KeepAlive = null;
        upstream.KeepAliveTimeout = null;
        Server.Names.Clear();
        Server.Listens.Clear();
        Server.TryFiles.Clear();
        Server.Ssl.Certificate = null;
        Server.Ssl.CertificateKey = null;
        Server.Ssl.Ciphers = null;
        Server.Ssl.PreferServerCiphers = null;
        Server.Ssl.SessionCache = null;
        Server.Ssl.SessionTimeout = null;
        Api.ProxyPass = null;
        Api.LimitExcept = null;
        Server.Locations.Last().Alias = null;

        Assert.That(_file.Serialize(), Is.EqualTo(Expected.PropertiesRemoved), "Only the empty blocks should be left");
    }

    [Test]
    [Description("A typed property throws, naming the directive and its line, when the file has a value it cannot read, e.g. a variable for a size.")]
    public void UnreadableThrows()
    {
        Server.Directives.Add(Consts.Sections.ClientMaxBodySize, Consts.Sections.SizeVariable);

        var exception = Assert.Throws<InvalidOperationException>(() => _ = Server.ClientMaxBodySize, "Reading a variable as a size should fail");

        Assert.That(exception.Message, Does.Contain(Consts.Sections.ClientMaxBodySize), "The message should name the directive");
    }
}
