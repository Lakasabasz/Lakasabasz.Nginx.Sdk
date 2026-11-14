namespace Lakasabasz.Nginx.Sdk.Tests.Infrastructure;

public static class NginxDump
{
    public static Task<IReadOnlyList<string>> LoadedFilesAsync() => LoadedFilesAsync([Consts.Files.DumpOption]);

    public static Task<IReadOnlyList<string>> LoadedFilesAsync(string mainFile) =>
        LoadedFilesAsync([Consts.Files.DumpOption, Consts.NginxOutput.ConfigOption, mainFile]);

    private static async Task<IReadOnlyList<string>> LoadedFilesAsync(string[] options)
    {
        var result = await NginxProcess.RunAsync(Consts.Paths.Sudo, [Consts.Arguments.NoPrompt, Consts.Paths.Binary, .. options]);
        return result.StandardOutput
            .Split(Consts.Files.NewLine)
            .Where(line => line.StartsWith(Consts.Files.DumpedFilePrefix, StringComparison.Ordinal)
                && line.EndsWith(Consts.Files.DumpedFileSuffix, StringComparison.Ordinal))
            .Select(line => line[Consts.Files.DumpedFilePrefix.Length..^Consts.Files.DumpedFileSuffix.Length])
            .Select(path => new FileInfo(path).ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? path)
            .ToList();
    }
}
