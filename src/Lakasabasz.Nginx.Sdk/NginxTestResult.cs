namespace Lakasabasz.Nginx.Sdk;

/// <summary>Result of a nginx config test (<c>nginx -t</c>). Warnings alone do not fail the test.</summary>
/// <param name="Success">nginx accepted the configuration.</param>
/// <param name="Errors">Diagnostics at <see cref="NginxLogLevel.Error"/> or above.</param>
/// <param name="Warnings">Diagnostics below <see cref="NginxLogLevel.Error"/>.</param>
public sealed record NginxTestResult(
    bool Success,
    IReadOnlyList<NginxDiagnostic> Errors,
    IReadOnlyList<NginxDiagnostic> Warnings);

/// <summary>One message reported by nginx, e.g. <c>[emerg] unknown directive "x" in /etc/nginx/nginx.conf:12</c>.</summary>
/// <param name="Level">Severity reported by nginx.</param>
/// <param name="Message">Message without the level and location.</param>
/// <param name="File">File the message points at, if any.</param>
/// <param name="Line">1-based line in <paramref name="File"/>, if any.</param>
public sealed record NginxDiagnostic(NginxLogLevel Level, string Message, string? File, int? Line)
{
    /// <summary>A diagnostic at <see cref="NginxLogLevel.Debug"/>.</summary>
    /// <param name="message">Message without the level and location.</param>
    /// <param name="file">File the message points at, if any.</param>
    /// <param name="line">1-based line in <paramref name="file"/>, if any.</param>
    public static NginxDiagnostic Debug(string message, string? file = null, int? line = null) =>
        new(NginxLogLevel.Debug, message, file, line);

    /// <summary>A diagnostic at <see cref="NginxLogLevel.Info"/>.</summary>
    /// <param name="message">Message without the level and location.</param>
    /// <param name="file">File the message points at, if any.</param>
    /// <param name="line">1-based line in <paramref name="file"/>, if any.</param>
    public static NginxDiagnostic Info(string message, string? file = null, int? line = null) =>
        new(NginxLogLevel.Info, message, file, line);

    /// <summary>A diagnostic at <see cref="NginxLogLevel.Notice"/>.</summary>
    /// <param name="message">Message without the level and location.</param>
    /// <param name="file">File the message points at, if any.</param>
    /// <param name="line">1-based line in <paramref name="file"/>, if any.</param>
    public static NginxDiagnostic Notice(string message, string? file = null, int? line = null) =>
        new(NginxLogLevel.Notice, message, file, line);

    /// <summary>A diagnostic at <see cref="NginxLogLevel.Warn"/>.</summary>
    /// <param name="message">Message without the level and location.</param>
    /// <param name="file">File the message points at, if any.</param>
    /// <param name="line">1-based line in <paramref name="file"/>, if any.</param>
    public static NginxDiagnostic Warn(string message, string? file = null, int? line = null) =>
        new(NginxLogLevel.Warn, message, file, line);

    /// <summary>A diagnostic at <see cref="NginxLogLevel.Error"/>.</summary>
    /// <param name="message">Message without the level and location.</param>
    /// <param name="file">File the message points at, if any.</param>
    /// <param name="line">1-based line in <paramref name="file"/>, if any.</param>
    public static NginxDiagnostic Error(string message, string? file = null, int? line = null) =>
        new(NginxLogLevel.Error, message, file, line);

    /// <summary>A diagnostic at <see cref="NginxLogLevel.Crit"/>.</summary>
    /// <param name="message">Message without the level and location.</param>
    /// <param name="file">File the message points at, if any.</param>
    /// <param name="line">1-based line in <paramref name="file"/>, if any.</param>
    public static NginxDiagnostic Crit(string message, string? file = null, int? line = null) =>
        new(NginxLogLevel.Crit, message, file, line);

    /// <summary>A diagnostic at <see cref="NginxLogLevel.Alert"/>.</summary>
    /// <param name="message">Message without the level and location.</param>
    /// <param name="file">File the message points at, if any.</param>
    /// <param name="line">1-based line in <paramref name="file"/>, if any.</param>
    public static NginxDiagnostic Alert(string message, string? file = null, int? line = null) =>
        new(NginxLogLevel.Alert, message, file, line);

    /// <summary>A diagnostic at <see cref="NginxLogLevel.Emerg"/>.</summary>
    /// <param name="message">Message without the level and location.</param>
    /// <param name="file">File the message points at, if any.</param>
    /// <param name="line">1-based line in <paramref name="file"/>, if any.</param>
    public static NginxDiagnostic Emerg(string message, string? file = null, int? line = null) =>
        new(NginxLogLevel.Emerg, message, file, line);
}

/// <summary>nginx log levels, from the least to the most severe.</summary>
public enum NginxLogLevel
{
    /// <summary><c>debug</c></summary>
    Debug,
    /// <summary><c>info</c></summary>
    Info,
    /// <summary><c>notice</c></summary>
    Notice,
    /// <summary><c>warn</c></summary>
    Warn,
    /// <summary><c>error</c></summary>
    Error,
    /// <summary><c>crit</c></summary>
    Crit,
    /// <summary><c>alert</c></summary>
    Alert,
    /// <summary><c>emerg</c></summary>
    Emerg,
}
