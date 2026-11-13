namespace Lakasabasz.Nginx.Sdk;

/// <summary>The config files of the installation that are loaded.</summary>
public sealed class NginxConfig
{
    internal NginxConfig(IReadOnlyList<NginxFile> files)
    {
        All = files;
    }

    /// <summary>
    /// Every loaded file, in loading order: after <c>Nginx.Load</c> the given file first, then the files it includes;
    /// after <c>Nginx.Init</c> none.
    /// </summary>
    public IReadOnlyList<NginxFile> All { get; }
}
