using System.Diagnostics;

namespace Lakasabasz.Nginx.Sdk.Tests.Infrastructure;

public sealed record NginxProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Success => ExitCode == 0;

    public override string ToString() => Consts.Messages.ProcessResult(ExitCode, StandardOutput, StandardError);
}

public static class NginxProcess
{
    public static async Task<NginxProcessResult> RunAsync(string binary, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo(binary)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = await StartRetryingWhileTextFileBusyAsync(startInfo);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var error = process.StandardError.ReadToEndAsync(timeout.Token);
        await process.WaitForExitAsync(timeout.Token);

        return new NginxProcessResult(
            process.ExitCode,
            await ReadUnlessDaemonHoldsPipe(output),
            await ReadUnlessDaemonHoldsPipe(error));
    }

    private const int TextFileBusy = 26;

    private static async Task<Process> StartRetryingWhileTextFileBusyAsync(ProcessStartInfo startInfo)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return Process.Start(startInfo) ?? throw new InvalidOperationException(Consts.Messages.CannotStart(startInfo.FileName));
            }
            catch (System.ComponentModel.Win32Exception e) when (e.NativeErrorCode == TextFileBusy && attempt < 20)
            {
                await Task.Delay(50);
            }
        }
    }

    private static async Task<string> ReadUnlessDaemonHoldsPipe(Task<string> read)
    {
        try
        {
            return await read.WaitAsync(TimeSpan.FromSeconds(1));
        }
        catch (TimeoutException)
        {
            return string.Empty;
        }
    }
}
