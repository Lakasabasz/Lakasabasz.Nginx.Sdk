namespace Lakasabasz.Nginx.Sdk.Tests.Infrastructure;

public static class NginxControl
{
    public static Task<NginxProcessResult> TestAsync() => SudoNginxAsync(Consts.NginxOutput.TestOption);

    public static Task<NginxProcessResult> TestAsync(NginxFile generated, string workDirectory)
    {
        var content = generated.Serialize();
        var main = Path.Combine(workDirectory, Consts.Sections.MainFile);
        if (content.Contains(Consts.Sections.HttpBlockStart, StringComparison.Ordinal))
        {
            File.WriteAllText(main, content);
        }
        else
        {
            var site = Path.Combine(workDirectory, Consts.Sections.SiteFile);
            File.WriteAllText(site, content);
            File.WriteAllText(main, Consts.Sections.MainIncluding(site));
        }
        return SudoNginxAsync(Consts.NginxOutput.TestOption, Consts.NginxOutput.ConfigOption, main);
    }

    public static async Task ReloadAsync()
    {
        var result = await SudoNginxAsync(Consts.Arguments.Signal, Consts.Arguments.Reload);
        if (!result.Success)
            throw new InvalidOperationException(Consts.Messages.CommandFailed(Consts.Paths.Binary, [Consts.Arguments.Signal, Consts.Arguments.Reload], result));
    }

    private static Task<NginxProcessResult> SudoNginxAsync(params string[] arguments) =>
        NginxProcess.RunAsync(Consts.Paths.Sudo, [Consts.Arguments.NoPrompt, Consts.Paths.Binary, .. arguments]);
}
