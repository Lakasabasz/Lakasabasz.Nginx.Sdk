namespace Lakasabasz.Nginx.Sdk.Tests.Infrastructure;

public static class Consts
{
    public static class Platform
    {
        public const string Linux = "linux";
    }

    public static class Paths
    {
        public const string Binary = "/usr/sbin/nginx";
        public const string ConfigRoot = "/etc/nginx";
        public const string PidFile = "/run/nginx.pid";
        public const string ErrorLog = "/var/log/nginx/error.log";

        public const string OfficialCorpus = "/opt/nginx-corpus/official";

        public const string BinaryWithSpaces = "/opt/nginx with spaces/nginx";

        public const string Backup = "/tmp/tests.bak";
        public const string LockFile = "/tmp/nginx-tests.lock";
        public const string InitLockFile = "/tmp/nginx-tests.init.lock";
        public const string WorkRoot = "/tmp/nginx-sdk-tests";

        public const string SkippedTests = "/tmp/nginx-sdk-tests.skipped";

        public const string Sudo = "sudo";
        public const string Rm = "/usr/bin/rm";
        public const string Cp = "/usr/bin/cp";

        public const string MissingBinary = "sbin/nginx";

        public const string UnlistedBinary = "nginx-unlisted";

        public const string WriteProbePrefix = ".write-test-";

        public static string ProcessStat(int pid) => $"/proc/{pid}/stat";
    }

    public static class Arguments
    {
        public const string NoPrompt = "-n";
        public const string Version = "-v";
        public const string Signal = "-s";
        public const string Quit = "quit";
        public const string Stop = "stop";
        public const string Reload = "reload";
        public const string RecursiveForce = "-rf";
        public const string Archive = "-a";
    }

    public static class Config
    {
        public const string BrokenFile = "conf.d/broken.conf";
        public const string BrokenContent = "# broken on purpose\nnot_a_directive on;\n";
        public const int BrokenLine = 2;

        public const string DuplicateServerFile = "conf.d/duplicate.conf";
        public const string DuplicateServerContent = "server { listen 80; server_name reports.example.test; }\n";

        public const string ReportsSite = "sites-available/reports";
        public const string ReportsHealthReturn = "\"reports\\n\"";
        public const string ReloadedHealthReturn = "\"reloaded by the package\\n\"";
    }

    public static class Files
    {
        public const string DumpOption = "-T";
        public const string DumpedFilePrefix = "# configuration file ";
        public const string DumpedFileSuffix = ":";
        public const char NewLine = '\n';

        public const string MainPath = "/etc/nginx/nginx.conf";
        public const string MissingPath = "/etc/nginx/missing.conf";
        public const string SitesAvailablePath = "/etc/nginx/sites-available";
        public const string DefaultSite = "default";
        public const string LegacySite = "legacy";
        public const string ReportsSite = "reports";
        public const string NewSite = "shop";
        public const string MissingSite = "missing";
        public const string ReportsLink = "sites-enabled/reports";
        public const string LegacyLinkPath = "/etc/nginx/sites-enabled/legacy";
        public const string ReportsRelativePath = "sites-available/reports";
        public const string LegacyRelativePath = "sites-available/legacy";
        public const string Upstreams = "upstreams.conf";
        public const string ProxyHeaders = "proxy-headers.conf";
        public const string OfficialDefault = "default.conf";
        public const string NotAFileName = "conf.d/upstreams.conf";

        public const string AllConfFiles = "*.conf";
        public const string ConfigDirectoryFiles = "conf.d/*";
        public const string SitesAvailableFiles = "sites-available/*";
        public const string SitesEnabledFiles = "sites-enabled/*";
        public const string ConfExtension = ".conf";

        public const string Server = "server";
        public const string Listen = "listen";
        public const string ShopPort = "8097";
        public const string ShopContent = "server {\n    listen 8097;\n}\n";

        public const string OfficialCopy = "official";
        public const string OfficialMain = "nginx.conf";
        public const string AbsoluteConfigRoot = "/etc/nginx/";
        public const string OfficialUser = "user  nginx;";
        public const string ImageUser = "user  www-data;";

        public const string OrphanName = "orphan";
        public const string OrphanSite = "sites-available/orphan";
        public const string OrphanContent = "server {\n    listen 8096;\n    include snippets/missing.conf;\n}\n";
        public const string BrokenSite = "sites-available/broken";
        public const string BrokenSiteContent = "server {\n    listen 8095;\n";
        public const string BrokenSnippet = "snippets/broken.conf";
        public const string BrokenSnippetContent = "proxy_set_header Host $host\n";
        public const string GhostLink = "sites-enabled/ghost";
        public const string GhostTarget = "sites-available/ghost";
        public const string PlainEnabledSite = "sites-enabled/plain";
        public const string PlainEnabledContent = "server {\n    listen 8094;\n}\n";
    }

    public static class Fixtures
    {
        public const string Root = "Fixtures";
        public const string Arguments = "Parsing/arguments.conf";
        public const string Lua = "Parsing/lua.conf";
        public const string Crlf = "Parsing/crlf.conf";
        public const string NoFinalNewline = "Parsing/no-final-newline.conf";
        public const string Errors = "Errors";
        public const string Site = "Directives/site.conf";
        public const string SiteAdded = "Directives/site.added.conf";
        public const string SiteSet = "Directives/site.set.conf";
        public const string SiteSetDuplicates = "Directives/site.set-duplicates.conf";
        public const string SiteRemoved = "Directives/site.removed.conf";
        public const string SiteBlock = "Directives/site.block.conf";
        public const string SiteQuoted = "Directives/site.quoted.conf";
        public const string SiteTopLevel = "Directives/site.top-level.conf";
        public const string Inline = "Directives/inline.conf";
        public const string InlineAdded = "Directives/inline.added.conf";
        public const string CrlfAdded = "Directives/crlf.added.conf";
        public const string Values = "Directives/values.conf";

        public const string Properties = "Sections/properties.conf";
        public const string PropertiesRemoved = "Sections/properties.removed.conf";
        public const string PropertiesRenamed = "Sections/properties.renamed.conf";
        public const string Main = "Sections/main.conf";
        public const string MainAdded = "Sections/main.added.conf";
        public const string Names = "Sections/names.conf";
        public const string NamesChanged = "Sections/names.changed.conf";
        public const string ListenReplaced = "Sections/names.listen-replaced.conf";
        public const string Custom = "Sections/custom.conf";
        public const string CustomKept = "Sections/custom.kept.conf";
        public const string Mixed = "Sections/mixed.conf";
        public const string MixedChanged = "Sections/mixed.changed.conf";
        public const string MixedRelaid = "Sections/mixed.relaid.conf";
        public const string MixedRestyled = "Sections/mixed.restyled.conf";
        public const string MixedOneLineAdded = "Sections/mixed.one-line-added.conf";
        public const string MixedLooseAdded = "Sections/mixed.loose-added.conf";
        public const string Spaced = "Sections/spaced.conf";
        public const string Comments = "Sections/comments.conf";
        public const string CommentsChanged = "Sections/comments.changed.conf";
        public const string CommentsMoved = "Sections/comments.moved.conf";
        public const string CommentsUnfolded = "Sections/comments.unfolded.conf";
        public const string AttachAdded = "Sections/attach.added.conf";
        public const string AttachChanged = "Sections/attach.changed.conf";
        public const string TextServer = "Sections/text.server.conf";
        public const string TextLocationAndUpstream = "Sections/text.location-upstream.conf";
        public const string Switched = "Sections/switched.conf";
        public const string IndexExtended = "Sections/index.extended.conf";

        public const string ValuesTarget = "conf.d/values.conf";

        public const string BrokenDirectory = "broken";
        public const string BrokenMain = "main.conf";
        public const string Missing = "missing.conf";

        public static string BrokenMainContent(string directory) =>
            $"events {{}}\nhttp {{\n    include {directory}/broken/*.conf;\n    include {directory}/missing.conf;\n}}\n";
    }

    public static class Parsing
    {
        public const string Events = "events";
        public const string Http = "http";
        public const string Server = "server";
        public const string Location = "location";
        public const string Return = "return";
        public const string WorkerProcesses = "worker_processes";
        public const string LogFormat = "log_format";
        public const string ServerName = "server_name";
        public const string Set = "set";
        public const string AddHeader = "add_header";
        public const string ContentByLua = "content_by_lua_block";
        public const string ClientMaxBodySize = "client_max_body_size";
        public const string ProxyPass = "proxy_pass";
        public const string MultiAccept = "multi_accept";
        public const string User = "user";
        public const string On = "on";

        public static readonly string[] WorkerProcessesArguments = ["2"];
        public static readonly string[] LogFormatArguments = ["quoted", "$remote_addr \"$request\" ${status}"];
        public static readonly string[] ServerNameArguments = ["single quoted", "double quoted"];
        public static readonly string[] SetArguments = ["$suffix", "${host}-x"];
        public static readonly string[] CharsHeaderArguments = ["X-Chars", "a b;{}#", "always"];
        public static readonly string[] EscapesHeaderArguments = ["X-Escapes", "q\" b\\ t\t u\\d"];
        public static readonly string[] RegexLocationArguments = ["~", "\\.(png|jpg)$"];
        public static readonly string[] BracesReturnArguments = ["200", "a}b"];
        public static readonly string[] LuaReturnArguments = ["200"];
        public const int WorkerProcessesLine = 2;
        public const int CharsHeaderLine = 12;
        public const int CharsHeaderColumn = 3;
        public const string LuaBody = "\n\t\tlocal t = { a = \"}\" }  -- a } in a comment\n\t\tngx.say([[ } ]])\n\t";

        public static readonly string[] ClientMaxBodySizeArguments = ["10m"];
        public static readonly string[] NewServerNames = ["a.test", "b.test"];
        public static readonly string[] NewHeader = ["X-C", "c"];
        public static readonly string[] QuotedHeader = ["X-Q", "a b;{}"];
        public static readonly string[] ApiLocation = ["/api"];
        public static readonly string[] ApiProxy = ["http://127.0.0.1:5000"];
        public static readonly string[] UserArguments = ["www-data"];

        public const string TrickyBody = "x y;{}\"\\#'";
        public static readonly string[] TrickyStatusAndBody = ["200", TrickyBody];
        public const string PlainLocation = "/plain";
        public const int ValuesPort = 8091;
    }

    public static class ParseErrors
    {
        public const string AfterQuote = "after-quote.conf";
        public const string ExtraBrace = "extra-brace.conf";
        public const string MissingSemicolon = "missing-semicolon.conf";
        public const string Unclosed = "unclosed.conf";

        public static IReadOnlyList<NginxParseError> BrokenIncludes(string directory) =>
        [
            new(Path.Combine(directory, Fixtures.BrokenDirectory, AfterQuote), 2, 24, "unexpected \"b\""),
            new(Path.Combine(directory, Fixtures.BrokenDirectory, ExtraBrace), 3, 1, "unexpected \"}\""),
            new(Path.Combine(directory, Fixtures.BrokenDirectory, MissingSemicolon), 3, 1, "unexpected \"}\""),
            new(Path.Combine(directory, Fixtures.BrokenDirectory, Unclosed), 3, 1, "unexpected end of file, expecting \"}\""),
            new(Path.Combine(directory, Fixtures.BrokenMain), 4, 5,
                $"open() \"{Path.Combine(directory, Fixtures.Missing)}\" failed (2: No such file or directory)"),
        ];

        public static string NginxReport(NginxParseError error) => $"{error.Message} in {error.File}:{error.Line}";
    }

    public static class NginxOutput
    {
        public const string UnknownDirective = "unknown directive \"not_a_directive\"";
        public const string ConflictingServerName = "conflicting server name \"reports.example.test\" on 0.0.0.0:80, ignored";
        public const string PidFilePermissionDenied = "open() \"/run/nginx.pid\" failed (13: Permission denied)";
        public const string SudoPasswordRequired = "a password is required";

        public static string Version(Version version) => $"nginx/{version}";

        public const string TestSuccessful = "test is successful";
        public const string ConfigOption = "-c";
        public const string TestOption = "-t";
    }

    public static class NginxLogs
    {
        public static IReadOnlyList<NginxDiagnostic> UnknownDirectiveLogs =>
        [
            NginxDiagnostic.Emerg(NginxOutput.UnknownDirective, Path.Combine(Paths.ConfigRoot, Config.BrokenFile), Config.BrokenLine),
        ];

        public static IReadOnlyList<NginxDiagnostic> ConflictingServerNameLogs =>
        [
            NginxDiagnostic.Warn(NginxOutput.ConflictingServerName),
        ];

        public static IReadOnlyList<NginxDiagnostic> PidFilePermissionDeniedLogs =>
        [
            NginxDiagnostic.Emerg(NginxOutput.PidFilePermissionDenied),
        ];
    }

    public static class Http
    {
        public const string BaseAddress = "http://127.0.0.1";
        public const string HealthPath = "/health";
        public const string ReportsHost = "reports.example.test";
        public const string ReloadedHealth = "reloaded by the package\n";

        public static Uri Url(int port, string path) => new($"http://127.0.0.1:{port}{path}");
    }

    public static class Variables
    {
        public const string TestImage = "NGINX_SDK_TEST_IMAGE";
        public const string TestImageValue = "1";
        public const string BuddyUnitTestsToken = "BUDDY_UT_TOKEN";
        public const string BuddyUnitTestsServiceUrl = "BUDDY_UT_SERVICE_URL";
        public const string Buddy = "BUDDY";
        public const string BuddyValue = "true";
        public const string BuddyRunBranch = "BUDDY_RUN_BRANCH";
        public const string BuddyRunTag = "BUDDY_RUN_TAG";
        public const string BuddyRunRefType = "BUDDY_RUN_REF_TYPE";
        public const string BuddyRunRefName = "BUDDY_RUN_REF_NAME";
        public const string BuddyRunCommit = "BUDDY_RUN_COMMIT";
        public const string BuddyRunHash = "BUDDY_RUN_HASH";
        public const string BuddyActionRunHash = "BUDDY_ACTION_RUN_HASH";
    }

    public static class Sections
    {
        public const string SiteName = "sections-site";
        public const string OtherSiteName = "sections-other";
        public const string MainFile = "main.conf";
        public const string SiteFile = "site.conf";
        public const string HttpBlockStart = "http {";

        public const string CertificateDirectory = "/tmp/nginx-sdk-tests/certs";
        public const string CertificateStaging = "certs";
        public const string CertificateFile = "fullchain.pem";
        public const string CertificateKeyFile = "privkey.pem";
        public const string Certificate = "/tmp/nginx-sdk-tests/certs/fullchain.pem";
        public const string CertificateKey = "/tmp/nginx-sdk-tests/certs/privkey.pem";
        public const string CertificateSubject = "CN=example.test";
        public const int KeySize = 2048;

        public const string ServerBlock = "server";
        public const string LocationBlock = "location";
        public const string Listen = "listen";
        public const string ServerName = "server_name";
        public const string Index = "index";
        public const string ProxyPass = "proxy_pass";
        public const string Return = "return";
        public const string ClientMaxBodySize = "client_max_body_size";
        public const string SizeVariable = "$body_limit";

        public const string AttachName = "attach.test";
        public const string AttachAlias = "www.attach.test";
        public const int AttachPort = 8084;
        public const string RootPath = "/";
        public const string WebRoot = "/var/www";
        public const string OtherWebRoot = "/srv/www";

        public const int WorkerConnections = 512;
        public const string EventMethod = "epoll";
        public static readonly TimeSpan KeepAliveTimeout = TimeSpan.FromSeconds(65);
        public static readonly Size BodySize = Size.FromMegabytes(50);
        public const string DefaultType = "application/octet-stream";
        public const string HtmlRoot = "/var/www/html";
        public const string IndexHtml = "index.html";
        public const string IndexHtm = "index.htm";

        public const string SslServerName = "api.example.test";
        public const string PlainServerName = "plain.example.test";
        public const string AddedServerName = "new.example.test";
        public const string AddedUpstreamName = "added_backend";
        public const int AddedServerPort = 8089;

        public const string RenamedPath = "/v2";
        public const string RenamedUpstream = "api_backend";
        public const string RenamedUpstreamUrl = "http://api_backend";
        public static readonly string[] RenamedMethods = ["GET", "POST"];
        public const string GluedExact = "=/exact";
        public const string ExactPath = "/exact";
        public const string GluedRegex = "~\\.php$";
        public const string RegexPath = "\\.php$";
        public const string ProxyHeadersSnippet = "/etc/nginx/snippets/proxy-headers.conf";
        public const string FrameOptions = "X-Frame-Options";
        public const string Deny = "deny";
        public const string All = "all";
        public const string HostHeader = "Host";
        public const string HostHeaderLowerCase = "host";
        public const string HostVariable = "$host";
        public static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan ReadTimeout = TimeSpan.FromMinutes(2);
        public static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(30);
        public const string HttpVersion = "1.1";
        public const string HtmlType = "text/html";
        public static readonly IReadOnlyList<string> HtmlExtensions = ["html", "htm"];
        public const string UpstreamName = "backend";
        public static readonly UpstreamServer PrimaryBackend = new("127.0.0.1:5000") { Weight = 2, MaxFails = 3, FailTimeout = TimeSpan.FromSeconds(30) };
        public static readonly UpstreamServer BackupBackend = new("127.0.0.1:5001") { Backup = true };
        public const int UpstreamKeepAlive = 16;
        public static readonly TimeSpan UpstreamKeepAliveTimeout = TimeSpan.FromMinutes(1);
        public const string ExampleName = "example.test";
        public const string WwwName = "www.example.test";
        public static readonly Listen SslListen = new() { Port = 443, Ssl = true, DefaultServer = true };
        public static readonly Listen Ipv6SslListen = new() { Address = "[::]", Port = 443, Ssl = true };
        public static readonly string[] TryFiles = ["$uri", "$uri/", "=404"];
        public const string Ciphers = "HIGH:!aNULL:!MD5";
        public const string SessionCache = "shared:SSL:10m";
        public static readonly TimeSpan SessionTimeout = TimeSpan.FromDays(1);
        public const string ApiPath = "/api";
        public const string UpstreamUrl = "http://backend";
        public const string Get = "GET";
        public const string StaticPath = "/static/";
        public const string StaticRoot = "/var/www/static/";

        public static readonly string[] FileNames = ["first.test", "*.example.test", "second.test"];
        public const string WildcardName = "*.example.test";
        public const string MatchedByWildcard = "a.example.test";
        public const string AddedName = "added.test";
        public const string FirstLineName = "first.test";
        public const string RenamedName = "renamed.test";
        public static readonly Listen[] FileListens =
        [
            new() { Port = 80 },
            new() { Address = "127.0.0.1", Port = 8443, Ssl = true, Http2 = true },
            new() { Address = "[::]", Port = 8443, Ssl = true },
            new() { Address = "localhost" },
            new() { Address = "unix:/run/site.sock" },
        ];
        public static readonly int[] FilePorts = [80, 8443];
        public static readonly int[] FileSslPorts = [8443];
        public const int ReplacedPort = 8080;

        public const string ServerText = "server_name text.test;\nlisten 8085; # from text\n\n# the API\nlocation /api\n{\n    proxy_pass http://127.0.0.1:5000;\n}\n";
        public const string TextName = "text.test";
        public const string TextComment = "from text";
        public const string BackendUrl = "http://127.0.0.1:5000";
        public const string BackendAddress = "127.0.0.1:5000";
        public const string UpstreamText = "server 127.0.0.1:5000;";
        public const string HealthPath = "/health";
        public const string LocationText = "return 204;";
        public const string BrokenText = "listen 80;\nserver_name broken.test\n}\n";
        public static readonly (int Line, int Column) BrokenTextPosition = (3, 1);
        public const string BrokenTextMessage = "unexpected \"}\"";

        public const int SwitchPort = 8086;
        public const string SwitchListen = "8086";
        public const string SwitchName = "switch.test";
        public const string SwitchText = "listen 8086;\nserver_name switch.test;\nindex index.html;\n";
        public const string ApiText = "proxy_pass http://127.0.0.1:5000;";

        public const string CustomDirective = "more_set_headers";
        public static readonly string[] CustomArguments = ["Server: hidden", "X-Custom: on"];
        public const string CustomComment = "headers-more module";
        public const string CustomText = "more_set_headers   \"Server: hidden\"   \"X-Custom: on\" ;";

        public const string Crlf = "\r\n";
        public const string Lf = "\n";
        public const string Tab = "\t";
        public const string TwoSpaces = "  ";
        public const string NoContent = "204";

        public const string FileComment = "Example site\nmanaged by the SDK tests";
        public const string ServerComment = "main server";
        public const string NameComment = "the name";
        public const string StaticComment = "static files\nlocation /old { root /srv/old; }";
        public const string NewFileComment = "Rewritten header";
        public const string MixedHeader = "mixed layout";
        public const string FileCommentOnly = "# Example site\n# managed by the SDK tests\n\n";
        public const string NewPath = "/new";
        public const string AddedComment = "a new location\nwith two lines";
        public const string CarriageReturnComment = "first\r\nsecond";
        public const int WideSpacing = 2;
        public const string ListenComment = "public port";
        public const string LocationComment = "static files";
        public const string ApiComment = "API";
        public const string LineComment = "added in code";
        public const string TwoLineComment = "first\nsecond";

        public static readonly string[] Indexes = [IndexHtml, IndexHtm];
        public static readonly string[] Includes = [ProxyHeadersSnippet];
        public static readonly string[] Names = [ExampleName, WwwName];
        public static readonly Listen[] Listens = [SslListen, Ipv6SslListen];
        public static readonly string[] LimitedMethods = [Get];
        public static readonly UpstreamServer[] Backends = [PrimaryBackend, BackupBackend];
        public static readonly string[] TextNames = [TextName];
        public static readonly string[] SslServerNames = [SslServerName];
        public static readonly string[] MainServerNames = [SslServerName, PlainServerName];
        public static readonly string[] MainUpstreamNames = [UpstreamName, AddedUpstreamName];

        public static readonly NginxStyle MixedStyle = new() { LineBreak = Crlf, Indentation = Tab, Braces = BraceStyle.NewLine, BlockSpacing = 0 };
        public static readonly NginxStyle RelaidStyle = new() { LineBreak = Lf, Indentation = TwoSpaces, Braces = BraceStyle.EndOfLine, BlockSpacing = 2 };
        public static readonly NginxStyle TwoSpaceStyle = new() { Indentation = TwoSpaces };

        public static string MainIncluding(string site) => $"events {{}}\nhttp {{\n    include {site};\n}}\n";
    }

    public static class Values
    {
        public const string KeepAliveTimeout = "keepalive_timeout";
        public const string Location = "location";
        public const string Listen = "listen";
        public const int DefaultPort = 80;

        public static readonly string[] TimeTexts = ["65", "500ms", "90s", "2m", "3h", "2d", "1w", "1M", "1y", "1h30m", "1h 30m 15s"];
        public static readonly TimeSpan?[] Times =
        [
            TimeSpan.FromSeconds(65), TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(90), TimeSpan.FromMinutes(2),
            TimeSpan.FromHours(3), TimeSpan.FromDays(2), TimeSpan.FromDays(7), TimeSpan.FromDays(30), TimeSpan.FromDays(365),
            TimeSpan.FromMinutes(90), new TimeSpan(1, 30, 15),
        ];
        public static readonly string[] NotTimes = ["x", "5q", "s", "1.5s", "-1s", "99999999999999999y"];
        public static readonly TimeSpan[] TimesToWrite =
            [TimeSpan.Zero, TimeSpan.FromSeconds(65), TimeSpan.FromMinutes(90) + TimeSpan.FromMilliseconds(250), TimeSpan.FromDays(30)];
        public static readonly string[] WrittenTimes = ["0s", "1m5s", "1h30m250ms", "30d"];

        public static readonly string[] SizeTexts = ["0", "512", "8k", "8K", "50m", "50M", "1g", "1G"];
        public static readonly long[] SizeBytes = [0, 512, 8192, 8192, 52428800, 52428800, 1073741824, 1073741824];
        public static readonly string[] NotSizes = ["", "k", "-1", "+1", "1.5m", "10x", "99999999999g"];
        public static readonly Size[] SizesToWrite =
            [Size.FromBytes(0), Size.FromBytes(1536), Size.FromKilobytes(1024), Size.FromMegabytes(50), Size.FromGigabytes(2)];
        public static readonly string[] WrittenSizes = ["0", "1536", "1m", "50m", "2g"];

        public static readonly string[][] LocationArguments =
            [["/api"], ["=", "/exact"], ["=/glued"], ["^~/static/"], ["~*\\.png$"], ["~\\.php$"], ["~", "\\.js$"], ["@fallback"], ["="]];
        public static readonly (LocationModifier, string)[] LocationKeys =
        [
            (LocationModifier.None, "/api"), (LocationModifier.Exact, "/exact"), (LocationModifier.Exact, "/glued"),
            (LocationModifier.PreferPrefix, "/static/"), (LocationModifier.RegexIgnoreCase, "\\.png$"),
            (LocationModifier.Regex, "\\.php$"), (LocationModifier.Regex, "\\.js$"), (LocationModifier.None, "@fallback"),
            (LocationModifier.None, "="),
        ];

        public static readonly string[] FullListenArguments = ["127.0.0.1:443", "default", "ssl", "backlog=512", "http2", "ipv6only=off", "reuseport"];
        public static readonly Listen FullListen = new()
        {
            Address = "127.0.0.1", Port = 443, DefaultServer = true, Ssl = true, Http2 = true, ReusePort = true, Ipv6Only = false,
            Parameters = ["backlog=512"],
        };
        public static readonly string[] WrittenFullListen = ["127.0.0.1:443", "ssl", "http2", "default_server", "reuseport", "ipv6only=off", "backlog=512"];
        public static readonly string[] Ipv6WithoutPortArguments = ["[::1]"];
        public static readonly Listen Ipv6WithoutPort = new() { Address = "[::1]" };
    }

    public static class Formats
    {
        public const string CompactGuid = "N";
        public const char Space = ' ';
        public const string Paragraph = "\n\n";
        public const string LineEnd = "\n";
        public const string Description = "Description";
        public const string Template = "_template";

        public const char ProcessNameEnd = ')';

        public const char ZombieState = 'Z';
    }

    public static class Guard
    {
        public const string ExtensionVersion = "1.0.0";
        public const string ExtensionName = "Skipped tests guard";
        public const string ExtensionDescription = "Records every test skipped in the test image, where nothing may be skipped.";
    }

    public static class Buddy
    {
        public const string SessionsPath = "/unit-tests/sessions";
        public const string IdProperty = "id";
        public const string Bearer = "Bearer";
        public const char TokenSeparator = '_';
        public const string TokenKind = "ut";
        public const string EuRegion = "eu";
        public const string AsiaRegion = "asia";
        public const string EuUrl = "https://api.eu.buddy.works";
        public const string AsiaUrl = "https://api.asia.buddy.works";
        public const string DefaultUrl = "https://api.buddy.works";
        public const string NoRegion = "";
        public const string CiProvider = "BUDDY";
        public const string NoCiProvider = "NONE";
        public const string Branch = "BRANCH";
        public const string Tag = "TAG";
        public const string PullRequest = "PULL_REQUEST";
        public const string ExtensionVersion = "1.0.0";
        public const string ExtensionName = "Buddy unit test reporting";
        public const string ExtensionDescription = "Sends test results to a Buddy unit test suite every 5 seconds.";
        public const string DefaultTestGroup = "tests";
        public const string TimeoutResult = "Timeout";

        public static string CasesPath(string sessionId) => $"/unit-tests/sessions/{sessionId}/cases/batch";

        public static string ClosePath(string sessionId) => $"/unit-tests/sessions/{sessionId}/close";

        public static string Classname(string ns, string type) => $"{ns}.{type}";

        public static string TestGroup(string assembly, Version framework) => $"{assembly} (net{framework.Major}.{framework.Minor})";

        public static string Failure(string message, string? stackTrace) => $"{message}\n\n{stackTrace}".Trim();

        public static string Failure(string explanation, string message, string? stackTrace) =>
            $"{explanation}\n\n{message}\n\n{stackTrace}".Trim();

        public static string RequestFailed(HttpResponseMessage response, string body) =>
            $"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri?.AbsolutePath}: {(int)response.StatusCode} {body}";

        public static string CannotOpen(string error) => $"[buddy-tests] Cannot open a test session, results will not be reported: {error}";

        public static string CannotClose(string error) => $"[buddy-tests] Cannot close the test session: {error}";

        public static string NotReported(int count) => $"[buddy-tests] {count} test results were not reported to Buddy.";

        public static string SendFailed(int count, string error) =>
            $"[buddy-tests] Sending {count} test results failed, retrying with the next flush: {error}";
    }

    public static class Messages
    {
        public const string NeedsTestImage = "Needs nginx from the test image: run tests/run-in-docker.sh.";
        public const string NginxDidNotStart = "nginx did not start.";
        public const string ConditionNotMet = "The condition was not met within 5 seconds: ";

        public const string NginxNotUsable = "NGINX_SDK_TEST_IMAGE says this is the test image, but its nginx is not usable.";

        public static string NoBinary(string binary) => $"No nginx at {binary}.";

        public static string NoConfigRoot(string configRoot) => $"No {configRoot}.";

        public static string ConfigRootNotWritable(string configRoot) =>
            $"{configRoot} is not writable: the tests change it and restore it after every change.";

        public static string SudoNotAllowed(string binary, object result) => $"sudo -n {binary} is not allowed: {result}";

        public static string AccessTimeout(string holder, TimeSpan timeout, object mode, IEnumerable<string> holders) =>
            $"{holder} waited {timeout.TotalMinutes} min for {mode} access to nginx. Holders in this process: {string.Join(", ", holders)}";

        public static string Holder(string name, object mode) => $"{name} ({mode})";

        public static string LockTimeout(string path, TimeSpan timeout) => $"Waited {timeout.TotalMinutes} min for {path}.";

        public static string CommandFailed(string command, IEnumerable<string> arguments, object result) =>
            $"{command} {string.Join(Formats.Space, arguments)}: {result}";

        public static string CannotStart(string command) => $"Cannot start {command}";

        public static string ProcessResult(int exitCode, string output, string error) => $"exit {exitCode}\n{output}{error}";

        public static string TimeLimitExceeded(TimeSpan duration, TimeSpan limit) =>
            $"The test ran {duration:mm\\:ss}, longer than the limit of {limit:mm\\:ss}.";

        public static string SkippedTest(string test, string? explanation) =>
            $"{test}: {explanation?.ReplaceLineEndings(" ")}\n";

        public static string Violation(string file, int line, string text) => $"{file}:{line}: {text}";

        public static string NginxRejected(object result) => $"nginx -t should accept the generated config: {result}";

        public static string TypeWithBase(Type type) => $"{type.FullName} : {type.BaseType?.FullName}";

        public static string Method(Type type, string method) => $"{type.FullName}.{method}";
    }

    public static class Source
    {
        public const string ProjectFile = "Lakasabasz.Nginx.Sdk.Tests.csproj";
        public const string ConstsFile = "Consts.cs";
        public const string Pattern = "*.cs";
        public const string AnyFile = "*";
        public const string Dot = ".";
        public const string Obj = "obj";
        public const string Bin = "bin";
        public const char Quote = '"';
        public const char Apostrophe = '\'';
        public const char Backslash = '\\';
        public const char Slash = '/';
        public const char Star = '*';
        public const char At = '@';
        public const char Dollar = '$';
        public const char NewLine = '\n';
        public const char OpenParenthesis = '(';
        public const char CloseParenthesis = ')';
        public const char OpenBracket = '[';
        public const char CloseBracket = ']';
        public const char OpenBrace = '{';
        public const char CloseBrace = '}';
        public const char Comma = ',';
        public const string AssertThat = "Assert.That(";
        public const string AssumeThat = "Assume.That(";
        public const string AssertThrows = "Assert.Throws<";
        public const string EventuallyAsync = "NginxHttp.EventuallyAsync(";
        public const string DescriptionAttribute = "[Description(";
        public const string BlockCommentEnd = "*/";
        public const char Blank = ' ';
        public const string RawQuotes = "\"\"\"";
    }
}
