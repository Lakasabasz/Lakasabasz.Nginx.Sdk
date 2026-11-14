namespace Lakasabasz.Nginx.Sdk.Tests.Infrastructure;

public static class NginxCorpus
{
    public static IReadOnlyList<string> Files(string root) => Directory
        .EnumerateFiles(root, Consts.Source.AnyFile, SearchOption.AllDirectories)
        .Where(file => new FileInfo(file).LinkTarget is null && !Path.GetFileName(file).StartsWith(Consts.Source.Dot, StringComparison.Ordinal))
        .Order(StringComparer.Ordinal)
        .ToList();
}
