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

        public static string TypeWithBase(Type type) => $"{type.FullName} : {type.BaseType?.FullName}";

        public static string Method(Type type, string method) => $"{type.FullName}.{method}";
    }

    public static class Source
    {
        public const string ProjectFile = "Lakasabasz.Nginx.Sdk.Tests.csproj";
        public const string ConstsFile = "Consts.cs";
        public const string Pattern = "*.cs";
        public const string AnyFile = "*";
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
