using System.Text.RegularExpressions;

namespace Lakasabasz.Nginx.Sdk.Internal;

/// <summary>
/// The one model of an installation's config: the loaded files, keyed by their real path, the enabled sites and the
/// files created or deleted since loading. <see cref="NginxConfig"/>, <see cref="NginxDirectory"/> and
/// <see cref="NginxSitesEnabled"/> are views over it, so a change through one is seen by all.
/// </summary>
internal sealed class ConfigWorkspace
{
    public const string MainFile = "nginx.conf";
    public const string SitesAvailable = "sites-available";
    public const string SitesEnabled = "sites-enabled";
    public const string ConfigDirectory = "conf.d";
    public const string Snippets = "snippets";

    private readonly List<NginxFile> _files;
    private readonly HashSet<string> _created = new(StringComparer.Ordinal);
    private readonly HashSet<string> _deleted = new(StringComparer.Ordinal);
    private readonly HashSet<string> _linkedOnDisk = new(StringComparer.Ordinal);
    private readonly HashSet<string> _plainFilesEnabled = new(StringComparer.Ordinal);
    private readonly HashSet<string> _enabled = new(StringComparer.Ordinal);

    public ConfigWorkspace(string configRoot, IEnumerable<NginxFile> files)
    {
        ConfigRoot = configRoot;
        RealRoot = RealPath.Of(configRoot);
        _files = files.ToList();
        Files = _files.AsReadOnly();
        ReadEnabledSites();
    }

    public string ConfigRoot { get; }

    public string RealRoot { get; }

    /// <summary>Every loaded file, in loading order; files created later come last.</summary>
    public IReadOnlyList<NginxFile> Files { get; }

    /// <summary>Files created in this model and not written yet (for committing changes).</summary>
    public IReadOnlyCollection<string> Created => _created;

    /// <summary>Loaded files deleted in this model and not removed from disk yet (for committing changes).</summary>
    public IReadOnlyCollection<string> Deleted => _deleted;

    /// <summary>Real paths of the enabled sites in this model.</summary>
    public IReadOnlyCollection<string> Enabled => _enabled;

    /// <summary>Real paths of the sites enabled on disk by a link in <c>sites-enabled</c> (for committing changes).</summary>
    public IReadOnlyCollection<string> LinkedOnDisk => _linkedOnDisk;

    /// <summary>The real path of a path relative to the config root (or absolute), with links resolved.</summary>
    public string Resolve(string path) => RealPath.Of(Path.IsPathRooted(path) ? path : Path.Combine(ConfigRoot, path));

    public string DirectoryPath(string directory) => Resolve(directory);

    public NginxFile? Get(string realPath) => _files.FirstOrDefault(file => file.Path == realPath);

    public IEnumerable<NginxFile> In(string directory)
    {
        var path = DirectoryPath(directory);
        return _files.Where(file => Path.GetDirectoryName(file.Path) == path);
    }

    public NginxFile Load(string path)
    {
        var realPath = Resolve(path);
        if (Get(realPath) is { } loaded)
            return loaded;
        var file = ConfigLoader.Load(realPath, ConfigRoot, followIncludes: false).Single();
        _files.Add(file);
        return file;
    }

    public NginxFile Create(string realPath)
    {
        var onDisk = (File.Exists(realPath) || Directory.Exists(realPath)) && !_deleted.Contains(realPath);
        if (Get(realPath) is not null || onDisk)
            throw new InvalidOperationException($"{realPath} already exists: get it with the indexer or TryGet instead.");

        var file = NginxFile.Empty(realPath);
        _files.Add(file);
        _deleted.Remove(realPath);
        _created.Add(realPath);
        return file;
    }

    public void Delete(NginxFile file)
    {
        _files.Remove(file);
        _enabled.Remove(file.Path);
        if (!_created.Remove(file.Path))
            _deleted.Add(file.Path);
    }

    public IReadOnlyList<NginxFile> Find(string pattern)
    {
        var regex = GlobRegex(pattern);
        Func<NginxFile, string> subject = pattern.StartsWith("/", StringComparison.Ordinal)
            ? file => file.Path
            : pattern.IndexOf('/') >= 0
                ? file => RelativePath(file.Path)
                : file => Path.GetFileName(file.Path);
        return _files.Where(file => regex.IsMatch(subject(file))).ToList();
    }

    public IEnumerable<NginxFile> EnabledSites => _files.Where(file => _enabled.Contains(file.Path));

    public bool IsEnabled(NginxFile site) => _enabled.Contains(site.Path) && Get(site.Path) == site;

    public void Enable(NginxFile site)
    {
        if (Get(site.Path) != site)
            throw new ArgumentException($"{site.Path} is not loaded in this Nginx: only its own files can be enabled.", nameof(site));
        if (Path.GetDirectoryName(site.Path) != DirectoryPath(SitesAvailable))
            throw new ArgumentException($"{site.Path} is not in {SitesAvailable}: only sites there can be enabled.", nameof(site));
        _enabled.Add(site.Path);
    }

    public void Disable(NginxFile site)
    {
        if (_plainFilesEnabled.Contains(site.Path))
            throw new InvalidOperationException(
                $"{site.Path} is a file in {SitesEnabled}, not a link to a site: delete the file to disable it.");
        _enabled.Remove(site.Path);
    }

    private void ReadEnabledSites()
    {
        var directory = Path.Combine(ConfigRoot, SitesEnabled);
        if (!Directory.Exists(directory))
            return;
        foreach (var entry in Directory.EnumerateFileSystemEntries(directory).Where(entry => !Path.GetFileName(entry).StartsWith(".", StringComparison.Ordinal)))
        {
            var target = RealPath.Of(entry);
            if (IsLink(entry))
                _linkedOnDisk.Add(target);
            else
                _plainFilesEnabled.Add(target);
            _enabled.Add(target);
        }
    }

    private static bool IsLink(string path) => (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

    private string RelativePath(string path)
    {
        var root = RealRoot.EndsWith("/", StringComparison.Ordinal) ? RealRoot : RealRoot + "/";
        return path.StartsWith(root, StringComparison.Ordinal) ? path.Substring(root.Length) : path;
    }

    /// <summary>A glob as nginx and the shell read it: <c>*</c> and <c>?</c> do not cross <c>/</c>, <c>[…]</c> is a set.</summary>
    private static Regex GlobRegex(string pattern) => new(
        "^" + Regex.Escape(pattern).Replace(@"\*", "[^/]*").Replace(@"\?", "[^/]").Replace(@"\[", "[") + "$",
        RegexOptions.CultureInvariant);
}
