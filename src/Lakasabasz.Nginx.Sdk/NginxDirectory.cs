using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Lakasabasz.Nginx.Sdk.Internal;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>
/// The loaded files of one directory of the config root, e.g. <c>sites-available</c>: get, create and delete files
/// by name. There is no get-or-create: <see cref="Create"/> and the indexer say which one is meant.
/// </summary>
public sealed class NginxDirectory : IEnumerable<NginxFile>
{
    private readonly ConfigWorkspace _workspace;
    private readonly string _directory;

    internal NginxDirectory(ConfigWorkspace workspace, string directory)
    {
        _workspace = workspace;
        _directory = directory;
    }

    /// <summary>The loaded file called <paramref name="name"/> in this directory.</summary>
    /// <param name="name">The file name, e.g. <c>"reports"</c> or <c>"upstreams.conf"</c>.</param>
    /// <exception cref="KeyNotFoundException">No such file is loaded.</exception>
    public NginxFile this[string name] => TryGet(name, out var file)
        ? file
        : throw new KeyNotFoundException($"{_directory}/{name} is not loaded.");

    /// <summary>The loaded file called <paramref name="name"/> in this directory, if there is one.</summary>
    /// <param name="name">The file name.</param>
    /// <param name="file">The file, or <see langword="null"/>.</param>
    /// <returns>Whether the file is loaded.</returns>
    public bool TryGet(string name, [NotNullWhen(true)] out NginxFile? file)
    {
        file = _workspace.Get(PathOf(name));
        return file is not null;
    }

    /// <summary>
    /// A new, empty file in this directory, added to <c>Config.All</c>. It is written to disk only when the changes
    /// are applied.
    /// </summary>
    /// <param name="name">The file name. Nothing is added to it: in <c>conf.d</c> give the <c>.conf</c> nginx includes.</param>
    /// <exception cref="InvalidOperationException">The file is loaded already or exists on disk.</exception>
    public NginxFile Create(string name) => _workspace.Create(PathOf(name));

    /// <summary>
    /// Deletes a loaded file: it leaves <c>Config.All</c>, and an enabled site is disabled too. It is removed from disk
    /// only when the changes are applied.
    /// </summary>
    /// <param name="name">The file name.</param>
    /// <exception cref="KeyNotFoundException">No such file is loaded.</exception>
    public void Delete(string name) => _workspace.Delete(this[name]);

    /// <inheritdoc />
    public IEnumerator<NginxFile> GetEnumerator() => _workspace.In(_directory).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private string PathOf(string name)
    {
        if (string.IsNullOrEmpty(name) || name.IndexOf('/') >= 0 || name is "." or "..")
            throw new ArgumentException($"\"{name}\" is not a file name: give the name only, without a directory.", nameof(name));
        return Path.Combine(_workspace.DirectoryPath(_directory), name);
    }
}
