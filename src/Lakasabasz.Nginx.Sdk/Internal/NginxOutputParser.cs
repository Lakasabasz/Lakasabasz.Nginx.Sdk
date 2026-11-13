using System.Globalization;
using System.Text.RegularExpressions;

namespace Lakasabasz.Nginx.Sdk.Internal;

/// <summary>Parses what nginx prints to stderr.</summary>
internal static class NginxOutputParser
{
    // Before the error log is opened (or with -e):
    //   nginx: [emerg] unknown directive "x" in /etc/nginx/conf.d/a.conf:12
    // In the log format, e.g. when the error log is stderr (default of Ubuntu's nginx 1.24):
    //   2026/11/08 12:36:50 [warn] 7#7: conflicting server name "a" on 0.0.0.0:80, ignored
    private static readonly Regex DiagnosticRegex = new(
        @"^(?:nginx: |\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2} )\[(?<level>[a-z]+)\] (?:\d+#\d+: )?(?<message>.*?)(?: in (?<file>/\S+):(?<line>\d+))?$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    // nginx version: nginx/1.24.0 (Ubuntu)
    private static readonly Regex VersionRegex = new(@"nginx/(?<version>\d+\.\d+\.\d+)", RegexOptions.CultureInvariant);

    public static IReadOnlyList<NginxDiagnostic> ParseDiagnostics(string output) => DiagnosticRegex
        .Matches(output.Replace("\r", ""))
        .Cast<Match>()
        .Select(m => new NginxDiagnostic(
            ParseLevel(m.Groups["level"].Value),
            m.Groups["message"].Value,
            m.Groups["file"].Success ? m.Groups["file"].Value : null,
            m.Groups["line"].Success ? int.Parse(m.Groups["line"].Value, CultureInfo.InvariantCulture) : null))
        .ToList();

    public static NginxTestResult ParseTestResult(int exitCode, string output)
    {
        var diagnostics = ParseDiagnostics(output);
        return new NginxTestResult(
            exitCode == 0,
            diagnostics.Where(d => d.Level >= NginxLogLevel.Error).ToList(),
            diagnostics.Where(d => d.Level < NginxLogLevel.Error).ToList());
    }

    public static Version? ParseVersion(string output)
    {
        var match = VersionRegex.Match(output);
        return match.Success ? Version.Parse(match.Groups["version"].Value) : null;
    }

    private static NginxLogLevel ParseLevel(string level) =>
        Enum.TryParse<NginxLogLevel>(level, ignoreCase: true, out var parsed) ? parsed : NginxLogLevel.Error;
}
