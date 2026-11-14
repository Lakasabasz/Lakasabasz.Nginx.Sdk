using System.Text.RegularExpressions;

namespace Lakasabasz.Nginx.Sdk.Internal;

/// <summary>
/// Loads config files and, optionally, the files their <c>include</c> directives name. Every file is parsed before
/// any error is reported, so one <see cref="NginxParseException"/> lists every broken file. A file is identified by
/// its real path: one reached through a link (an enabled site) is loaded once, as its target.
/// </summary>
internal static class ConfigLoader
{
    private const string Include = "include";
    private const string ConfFiles = "*.conf";

    /// <param name="path">The file to load; relative to <paramref name="configRoot"/> unless absolute.</param>
    /// <param name="configRoot">The directory relative include paths start from (nginx's conf directory).</param>
    /// <param name="followIncludes">Also load the files included, at any depth; each file once.</param>
    public static IReadOnlyList<NginxFile> Load(string path, string configRoot, bool followIncludes) =>
        Run(configRoot, Rooted(path, configRoot), followIncludes, []);

    /// <summary>
    /// Every config file of an installation: the main file with everything it includes (what nginx loads), then the
    /// files of <c>sites-available</c> (enabled or not), and the <c>*.conf</c> files of <c>conf.d</c> and
    /// <c>snippets</c>. Includes are followed only from the main file, as nginx does: a disabled site is loaded
    /// as it is.
    /// </summary>
    public static IReadOnlyList<NginxFile> Discover(string configRoot) => Run(
        configRoot,
        Rooted(ConfigWorkspace.MainFile, configRoot),
        followIncludes: true,
        Files(configRoot, ConfigWorkspace.SitesAvailable, null)
            .Concat(Files(configRoot, ConfigWorkspace.ConfigDirectory, ConfFiles))
            .Concat(Files(configRoot, ConfigWorkspace.Snippets, ConfFiles))
            .ToList());

    private static List<NginxFile> Run(string configRoot, string main, bool followIncludes, IReadOnlyList<string> others)
    {
        var files = new List<NginxFile>();
        var errors = new List<NginxParseError>();
        var loaded = new HashSet<string>(StringComparer.Ordinal);

        var pending = new Queue<(string Path, NginxParseError? MissingError)>();
        pending.Enqueue((main, null));
        while (pending.Count > 0)
        {
            var (path, missing) = pending.Dequeue();
            if (LoadOne(path, missing, files, errors, loaded) is not { } directives || !followIncludes)
                continue;
            foreach (var include in Includes(directives))
            {
                var target = Rooted(include.Arguments[0], configRoot);
                var matches = HasWildcards(target) ? Glob(target) : [target];
                foreach (var match in matches)
                    pending.Enqueue((match, new NginxParseError(path, include.Line, include.Column, NotFound(match))));
            }
        }

        foreach (var other in others)
            LoadOne(other, null, files, errors, loaded);

        if (errors.Count > 0)
            throw new NginxParseException(errors);
        return files;
    }

    /// <returns>The directives of the file when it was loaded now; null when loaded before, missing or broken.</returns>
    private static DirectiveCollection? LoadOne(string path, NginxParseError? missing, List<NginxFile> files,
        List<NginxParseError> errors, HashSet<string> loaded)
    {
        if (!RealPath.TryResolve(path, out var realPath) || Directory.Exists(realPath))
        {
            if (loaded.Add(path))
                errors.Add(missing ?? new NginxParseError(path, 0, 0, NotFound(path)));
            return null;
        }

        if (!loaded.Add(realPath))
            return null;

        var source = ConfigSource.Read(realPath);
        var (directives, error) = ConfigParser.Parse(realPath, source.Text);
        if (error is not null)
        {
            errors.Add(error);
            return null;
        }

        files.Add(new NginxFile(realPath, source, directives!));
        return directives;
    }

    private static IEnumerable<string> Files(string configRoot, string directory, string? pattern)
    {
        var path = Path.Combine(configRoot, directory);
        if (!Directory.Exists(path))
            return [];
        return Directory.EnumerateFiles(path, pattern ?? "*")
            .Where(file => !Path.GetFileName(file).StartsWith(".", StringComparison.Ordinal))
            .OrderBy(file => file, StringComparer.Ordinal);
    }

    private static IEnumerable<Directive> Includes(DirectiveCollection directives) => directives.SelectMany(directive =>
        directive.Name == Include && directive.Arguments.Count == 1 ? [directive] : Includes(directive.Directives));

    private static string Rooted(string path, string configRoot) =>
        Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(configRoot, path));

    private static string NotFound(string path) => $"open() \"{path}\" failed (2: No such file or directory)";

    private static bool HasWildcards(string path) => path.IndexOfAny(['*', '?', '[']) >= 0;

    /// <summary>
    /// Files matching a glob pattern in any path component, sorted like glob(3) does. Links count as files, also
    /// dangling ones (<see cref="File.Exists"/> is true for them), which nginx then fails to open.
    /// </summary>
    private static List<string> Glob(string pattern)
    {
        var parts = pattern.Split(Path.DirectorySeparatorChar);
        IEnumerable<string> current = [parts[0].Length == 0 ? Path.DirectorySeparatorChar.ToString() : parts[0]];
        for (var i = 1; i < parts.Length; i++)
        {
            var part = parts[i];
            var last = i == parts.Length - 1;
            current = current.SelectMany(directory =>
            {
                if (!Directory.Exists(directory))
                    return [];
                if (!HasWildcards(part))
                    return [Path.Combine(directory, part)];
                var regex = new Regex("^" + Regex.Escape(part).Replace(@"\*", ".*").Replace(@"\?", ".").Replace(@"\[", "[") + "$");
                return Directory.EnumerateFileSystemEntries(directory)
                    .Where(entry => regex.IsMatch(Path.GetFileName(entry)))
                    .Where(entry => !last || File.Exists(entry))
                    .OrderBy(entry => entry, StringComparer.Ordinal);
            });
        }
        return current.Where(File.Exists).ToList();
    }
}
