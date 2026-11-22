using Lakasabasz.Nginx.Sdk.Internal;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>
/// A path on the server: a file or directory, absolute (<c>/etc/nginx/nginx.conf</c>) or relative
/// (<c>sites-available/default</c>), compared by its text. Absolute paths are normalized: <c>.</c>, <c>..</c>,
/// double and trailing <c>/</c> are removed. A string converts to it, so <c>Config["sites-available/default"]</c>
/// works.
/// </summary>
public sealed class NginxPath : IEquatable<NginxPath>
{
    private const char Separator = '/';
    private const string Root = "/";
    private const string HiddenPrefix = ".";
    private static readonly char[] Wildcards = ['*', '?', '['];

    private readonly string _value;

    /// <summary>A path from its text.</summary>
    /// <param name="path">The path, absolute or relative.</param>
    /// <exception cref="ArgumentException">The path is empty or contains a null character.</exception>
    public NginxPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("A path cannot be empty.", nameof(path));
        if (path.IndexOf('\0') >= 0)
            throw new ArgumentException("A path cannot contain a null character.", nameof(path));
        _value = Normalize(path);
    }

    /// <summary>Whether the path starts at <c>/</c>.</summary>
    public bool IsAbsolute => _value[0] == Separator;

    /// <summary>The last part of the path: the file name, e.g. <c>default</c> for <c>sites-available/default</c>.</summary>
    public string Name => Path.GetFileName(_value);

    /// <summary>The directory the path is in; <see langword="null"/> for <c>/</c> and for a relative path of one part.</summary>
    public NginxPath? Parent => Path.GetDirectoryName(_value) is { Length: > 0 } parent ? new NginxPath(parent) : null;

    /// <summary>Whether the file name starts with a dot, as hidden files and editor backups do.</summary>
    public bool IsHidden => Name.StartsWith(HiddenPrefix, StringComparison.Ordinal);

    /// <summary>Whether the path is a glob: it contains <c>*</c>, <c>?</c> or <c>[</c>.</summary>
    public bool HasWildcards => _value.IndexOfAny(Wildcards) >= 0;

    internal bool IsFile => File.Exists(_value);

    internal bool IsDirectory => Directory.Exists(_value);

    internal bool IsLink => (IsFile || IsDirectory) && (File.GetAttributes(_value) & FileAttributes.ReparsePoint) != 0;

    /// <summary>A path from its text; see <see cref="NginxPath(string)"/>.</summary>
    /// <param name="path">The path, absolute or relative.</param>
    public static implicit operator NginxPath(string path) => new(path);

    /// <summary>Whether both paths have the same text.</summary>
    public static bool operator ==(NginxPath? left, NginxPath? right) => Equals(left, right);

    /// <summary>Whether the paths have different text.</summary>
    public static bool operator !=(NginxPath? left, NginxPath? right) => !Equals(left, right);

    /// <summary>A path from its text; the named form of the conversion from a string.</summary>
    /// <param name="path">The path, absolute or relative.</param>
    public static NginxPath FromString(string path) => new(path);

    /// <summary>This path with <paramref name="path"/> added at the end, e.g. <c>/etc/nginx</c> + <c>conf.d</c>.</summary>
    /// <param name="path">A relative path.</param>
    /// <exception cref="ArgumentException"><paramref name="path"/> is absolute, so it cannot continue this one.</exception>
    public NginxPath Append(NginxPath path) => path.IsAbsolute
        ? throw new ArgumentException($"{path} is absolute: it cannot be appended to {this}. Use CollideAppend to let it win.", nameof(path))
        : new NginxPath(_value + Separator + path._value);

    /// <summary>
    /// <see cref="Append"/> as nginx reads a path in its config: a relative <paramref name="path"/> is added at the end
    /// of this one, an absolute <paramref name="path"/> wins and this one is ignored.
    /// </summary>
    /// <param name="path">A relative or absolute path.</param>
    public NginxPath CollideAppend(NginxPath path) => path.IsAbsolute ? path : Append(path);

    /// <summary>This path relative to <paramref name="directory"/>; the path itself when it is not inside it.</summary>
    /// <param name="directory">The directory.</param>
    public NginxPath RelativeTo(NginxPath directory)
    {
        var prefix = directory._value == Root ? Root : directory._value + Separator;
        return _value.Length > prefix.Length && _value.StartsWith(prefix, StringComparison.Ordinal)
            ? new NginxPath(_value.Substring(prefix.Length))
            : this;
    }

    /// <summary>Whether the path is directly in <paramref name="directory"/>, not deeper.</summary>
    /// <param name="directory">The directory.</param>
    public bool IsIn(NginxPath directory) => Parent == directory;

    /// <inheritdoc />
    public bool Equals(NginxPath? other) => other is not null && string.Equals(_value, other._value, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is NginxPath other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(_value);

    /// <summary>The text of the path.</summary>
    public override string ToString() => _value;

    /// <summary>
    /// The path with every link resolved (realpath(3)), so a file reached through a link, such as an enabled site, is
    /// its target. False for a missing file and a link to one (a dangling link).
    /// </summary>
    /// <param name="real">The resolved path; this path, made absolute, when it cannot be resolved.</param>
    internal bool TryResolveLinks(out NginxPath real)
    {
        var resolved = LibC.TryRealPath(_value, out var text);
        real = new NginxPath(text);
        return resolved;
    }

    /// <summary><see cref="TryResolveLinks"/>, or this path made absolute when it cannot be resolved.</summary>
    internal NginxPath ResolveLinks()
    {
        TryResolveLinks(out var real);
        return real;
    }

    /// <summary>The files in this directory matching a file name pattern, without hidden files, sorted by name.</summary>
    /// <param name="pattern">A file name pattern, e.g. <c>*.conf</c>.</param>
    internal IEnumerable<NginxPath> Files(string pattern) => IsDirectory
        ? Directory.EnumerateFiles(_value, pattern).Select(FromString).Where(file => !file.IsHidden).OrderBy(file => file._value, StringComparer.Ordinal)
        : [];

    /// <summary>
    /// Everything in this directory (files, directories and links, also dangling ones), without hidden entries, sorted
    /// by name.
    /// </summary>
    internal IEnumerable<NginxPath> Entries() => IsDirectory
        ? Directory.EnumerateFileSystemEntries(_value).Select(FromString).Where(entry => !entry.IsHidden).OrderBy(entry => entry._value, StringComparer.Ordinal)
        : [];

    private static string Normalize(string path)
    {
        if (path[0] != Separator)
            return path.Length > 1 ? path.TrimEnd(Separator) : path;
        var full = Path.GetFullPath(path);
        return full.Length > 1 ? full.TrimEnd(Separator) : full;
    }
}
