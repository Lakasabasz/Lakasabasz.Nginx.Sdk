using System.Text.RegularExpressions;

namespace Lakasabasz.Nginx.Sdk.Internal;

/// <summary>
/// Loads config files and, optionally, the files their <c>include</c> directives name. Every file is parsed before
/// any error is reported, so one <see cref="NginxParseException"/> lists every broken file.
/// </summary>
internal static class ConfigLoader
{
    private const string Include = "include";

    /// <param name="path">The file to load; relative to <paramref name="configRoot"/> unless absolute.</param>
    /// <param name="configRoot">The directory relative include paths start from (nginx's conf directory).</param>
    /// <param name="followIncludes">Also load the files included, at any depth; each file once.</param>
    public static IReadOnlyList<NginxFile> Load(string path, string configRoot, bool followIncludes)
    {
        var files = new List<NginxFile>();
        var errors = new List<NginxParseError>();
        var loaded = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Queue<(string Path, NginxParseError? MissingError)>();
        pending.Enqueue((Resolve(path, configRoot), null));

        while (pending.Count > 0)
        {
            var (file, missing) = pending.Dequeue();
            if (!loaded.Add(file))
                continue;
            if (!File.Exists(file))
            {
                errors.Add(missing ?? new NginxParseError(file, 0, 0, NotFound(file)));
                continue;
            }

            var source = ConfigSource.Read(file);
            var (directives, error) = ConfigParser.Parse(file, source.Text);
            if (error is not null)
            {
                errors.Add(error);
                continue;
            }

            files.Add(new NginxFile(file, source, directives!));
            if (!followIncludes)
                continue;
            foreach (var include in Includes(directives!))
            {
                var target = Resolve(include.Arguments[0], configRoot);
                if (HasWildcards(target))
                {
                    foreach (var match in Glob(target))
                        pending.Enqueue((match, null));
                }
                else
                {
                    pending.Enqueue((target, new NginxParseError(file, include.Line, include.Column, NotFound(target))));
                }
            }
        }

        if (errors.Count > 0)
            throw new NginxParseException(errors);
        return files;
    }

    private static IEnumerable<Directive> Includes(DirectiveCollection directives) => directives.SelectMany(directive =>
        directive.Name == Include && directive.Arguments.Count == 1 ? [directive] : Includes(directive.Directives));

    private static string Resolve(string path, string configRoot) =>
        Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(configRoot, path));

    private static string NotFound(string path) => $"open() \"{path}\" failed (2: No such file or directory)";

    private static bool HasWildcards(string path) => path.IndexOfAny(['*', '?', '[']) >= 0;

    /// <summary>Files matching a glob pattern in any path component, sorted like glob(3) does.</summary>
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
