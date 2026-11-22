namespace Lakasabasz.Nginx.Sdk.Tests.Infrastructure;

public static class NginxControl
{
    public static Task<NginxProcessResult> TestAsync() => SudoNginxAsync(Consts.NginxOutput.TestOption);

    public static async Task ReloadAsync()
    {
        var result = await SudoNginxAsync(Consts.Arguments.Signal, Consts.Arguments.Reload);
        if (!result.Success)
            throw new InvalidOperationException(Consts.Messages.CommandFailed(Consts.Paths.Binary, [Consts.Arguments.Signal, Consts.Arguments.Reload], result));
    }

    private static Task<NginxProcessResult> SudoNginxAsync(params string[] arguments) =>
        NginxProcess.RunAsync(Consts.Paths.Sudo, [Consts.Arguments.NoPrompt, Consts.Paths.Binary, .. arguments]);
}
