namespace Lakasabasz.Nginx.Sdk.Internal;

/// <summary>
/// The config files of an installation: the loaded files, keyed by their real path, and the files created or deleted
/// since loading. <see cref="NginxConfig"/> and <see cref="NginxDirectory"/> are views over it, so a change through one
/// is seen by both. Which sites are enabled is not here: that is <see cref="SiteLinks"/>.
/// </summary>
internal sealed class ConfigWorkspace
{
    private readonly List<ConfigDocument> _files = [];
    private readonly HashSet<NginxPath> _created = [];
    private readonly HashSet<NginxPath> _deleted = [];
    private readonly Action<NginxPath> _onDeleted;

    /// <summary>A workspace with no files; <see cref="ConfigDiscovery"/> and <see cref="Load"/> add them.</summary>
    /// <param name="configRoot">nginx's config directory.</param>
    /// <param name="onDeleted">Told the real path of every file deleted, e.g. to remove a link to it.</param>
    public ConfigWorkspace(NginxPath configRoot, Action<NginxPath> onDeleted)
    {
        ConfigRoot = configRoot;
        RealConfigRoot = configRoot.ResolveLinks();
        Files = _files.AsReadOnly();
        _onDeleted = onDeleted;
    }

    /// <summary>nginx's config directory, as given; relative paths start here.</summary>
    public NginxPath ConfigRoot { get; }

    /// <summary><see cref="ConfigRoot"/> with every link resolved.</summary>
    public NginxPath RealConfigRoot { get; }

    /// <summary>Every loaded file, in loading order; files created later come last.</summary>
    public IReadOnlyList<ConfigDocument> Files { get; }

    /// <summary>The real path of a path relative to the config root (or absolute): the path a file is identified by.</summary>
    public NginxPath Resolve(NginxPath path) => ConfigRoot.CollideAppend(path).ResolveLinks();

    public ConfigDocument? Get(NginxPath realPath) => _files.FirstOrDefault(file => file.Path == realPath);

    public IEnumerable<ConfigDocument> In(NginxPath directory)
    {
        var path = Resolve(directory);
        return _files.Where(file => file.Path.IsIn(path));
    }

    /// <summary>Loads one file (not the files it includes), unless it is loaded already.</summary>
    /// <param name="path">Relative to the config root, or absolute.</param>
    /// <exception cref="NginxParseException">The file does not exist or has a syntax error.</exception>
    public ConfigDocument Load(NginxPath path)
    {
        var realPath = Resolve(path);
        if (Get(realPath) is { } loaded)
            return loaded;
        var file = ConfigDocument.Load(realPath);
        _files.Add(file);
        return file;
    }

    /// <summary>Adds a file read from disk (used by <see cref="ConfigDiscovery"/>).</summary>
    public void Add(ConfigDocument file) => _files.Add(file);

    public ConfigDocument Create(NginxPath realPath)
    {
        var onDisk = (realPath.IsFile || realPath.IsDirectory) && !_deleted.Contains(realPath);
        if (Get(realPath) is not null || onDisk)
            throw new InvalidOperationException($"{realPath} already exists: get it with the indexer or TryGet instead.");

        var file = ConfigDocument.Empty(realPath);
        _files.Add(file);
        _deleted.Remove(realPath);
        _created.Add(realPath);
        return file;
    }

    public void Delete(ConfigDocument file)
    {
        _files.Remove(file);
        if (!_created.Remove(file.Path))
            _deleted.Add(file.Path);
        _onDeleted(file.Path);
    }

    public IReadOnlyList<ConfigDocument> Find(string pattern)
    {
        Func<ConfigDocument, string> subject = pattern.StartsWith("/", StringComparison.Ordinal)
            ? file => file.Path.ToString()
            : pattern.IndexOf('/') >= 0
                ? file => file.Path.RelativeTo(RealConfigRoot).ToString()
                : file => file.Path.Name;
        return _files.Where(file => LibC.FnMatch(pattern, subject(file))).ToList();
    }
}
