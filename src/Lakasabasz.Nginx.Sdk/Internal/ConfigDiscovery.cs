namespace Lakasabasz.Nginx.Sdk.Internal;

/// <summary>
/// Builds the list of files of a <see cref="ConfigWorkspace"/>: which files to load and in which order. Each file is
/// read by <see cref="ConfigDocument.Read"/>; a missing or broken file does not stop the others, and one
/// <see cref="NginxParseException"/> at the end lists every broken file.
/// </summary>
internal static class ConfigDiscovery
{
    private const string ConfFiles = "*.conf";
    private const string AllFiles = "*";

    /// <summary>
    /// Every config file of the installation: <c>nginx.conf</c> with everything it includes (what nginx loads), then
    /// the files of <c>sites-available</c> (enabled or not), and the <c>*.conf</c> files of <c>conf.d</c> and
    /// <c>snippets</c>. Includes are followed only from <c>nginx.conf</c>, as nginx does: a disabled site is loaded as
    /// it is.
    /// </summary>
    /// <exception cref="NginxParseException">Some files are missing or broken; the others are loaded.</exception>
    public static void Discover(this ConfigWorkspace workspace)
    {
        var root = workspace.ConfigRoot;
        var directoryFiles = root.Append(ConfigLayout.SitesAvailable).Files(AllFiles)
            .Concat(root.Append(ConfigLayout.ConfigDirectory).Files(ConfFiles))
            .Concat(root.Append(ConfigLayout.Snippets).Files(ConfFiles));

        var batch = new Batch(workspace);
        batch.AddIncludeChain(root.Append(ConfigLayout.MainFile));
        foreach (var file in directoryFiles)
            batch.Add(file);
        batch.ThrowIfFailed();
    }

    /// <summary>A file and every file it includes, at any depth (globs are expanded, each file is loaded once).</summary>
    /// <param name="workspace">The workspace to load into.</param>
    /// <param name="start">The first file; relative to the config root unless absolute.</param>
    /// <exception cref="NginxParseException">Some files are missing or broken; the others are loaded.</exception>
    public static void LoadIncludeChain(this ConfigWorkspace workspace, NginxPath start)
    {
        var batch = new Batch(workspace);
        batch.AddIncludeChain(workspace.ConfigRoot.CollideAppend(start));
        batch.ThrowIfFailed();
    }

    /// <summary>One loading: the files tried so far and the errors collected.</summary>
    private sealed class Batch(ConfigWorkspace workspace)
    {
        private readonly HashSet<NginxPath> _tried = [];
        private readonly List<NginxParseError> _errors = [];

        /// <summary>Loads a file into the workspace unless it was tried already.</summary>
        /// <param name="path">The file.</param>
        /// <param name="whenMissing">The error to report when the file does not exist, e.g. at the include naming it.</param>
        /// <returns>The file when it was loaded now; null when tried before, missing or broken.</returns>
        public ConfigDocument? Add(NginxPath path, NginxParseError? whenMissing = null)
        {
            var exists = path.TryResolveLinks(out var realPath);
            if (!_tried.Add(exists ? realPath : path))
                return null;

            var result = ConfigDocument.Read(path);
            if (!result.IsSuccess)
            {
                var missing = !exists || realPath.IsDirectory;
                _errors.Add(missing && whenMissing is not null ? whenMissing : result.Error);
                return null;
            }

            workspace.Add(result.Value);
            return result.Value;
        }

        /// <summary>The file and the files its includes name, breadth first.</summary>
        public void AddIncludeChain(NginxPath start)
        {
            var pending = new Queue<(NginxPath Path, NginxParseError? WhenMissing)>();
            pending.Enqueue((start, null));
            while (pending.Count > 0)
            {
                var (path, whenMissing) = pending.Dequeue();
                if (Add(path, whenMissing) is not { } file)
                    continue;
                foreach (var include in file.Tree.Includes())
                {
                    var target = workspace.ConfigRoot.CollideAppend(include.Arguments[0]);
                    foreach (var match in target.HasWildcards ? ExpandGlob(target) : [target])
                        pending.Enqueue((match, NginxParseError.NotFound(match, file.Path, include.Line, include.Column)));
                }
            }
        }

        public void ThrowIfFailed()
        {
            if (_errors.Count > 0)
                throw new NginxParseException(_errors);
        }
    }

    /// <summary>The files an <c>include</c> pattern names, expanded by glob(3) as nginx does.</summary>
    private static IEnumerable<NginxPath> ExpandGlob(NginxPath pattern) => LibC.Glob(pattern.ToString()).Select(NginxPath.FromString);
}
