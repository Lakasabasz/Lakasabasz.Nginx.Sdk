using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Lakasabasz.Nginx.Sdk.Internal;

internal sealed record NginxCommandResult(string Command, int ExitCode, string StandardOutput, string StandardError)
{
    public string Output => StandardOutput + StandardError;
}

/// <summary>Runs the nginx binary of an <see cref="NginxEnvironment"/>, through <c>sudo -n</c> when requested.</summary>
internal static class NginxCommand
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    public static NginxCommandResult Run(NginxEnvironment environment, params string[] arguments)
    {
        var (fileName, allArguments) = environment.Sudo
            ? ("sudo", new[] { "-n", environment.Binary }.Concat(arguments).ToArray())
            : (environment.Binary, arguments);
        var commandLine = JoinArguments(allArguments);
        var command = fileName + " " + commandLine;

        var startInfo = new ProcessStartInfo(fileName, commandLine)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        Process process;
        try
        {
            process = Process.Start(startInfo)!;
        }
        catch (Win32Exception e)
        {
            throw new NginxCommandException($"Cannot run {fileName}: {e.Message}", command, null, "", e);
        }

        using (process)
        {
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit((int)Timeout.TotalMilliseconds))
            {
                process.Kill();
                throw new NginxCommandException($"{command} did not finish within {Timeout.TotalSeconds}s", command, null, "");
            }

            var result = new NginxCommandResult(command, process.ExitCode, output.Result, error.Result);
            if (environment.Sudo && result.ExitCode != 0 && result.StandardError.Contains("sudo:"))
                throw new NginxCommandException($"sudo refused to run nginx: {result.StandardError.Trim()}", command, result.ExitCode, result.Output);
            return result;
        }
    }

    /// <summary>
    /// Builds <see cref="ProcessStartInfo.Arguments"/> so that each argument arrives unchanged
    /// (netstandard2.0 has no ArgumentList).
    /// </summary>
    internal static string JoinArguments(IEnumerable<string> arguments) =>
        string.Join(" ", arguments.Select(Quote));

    private static string Quote(string argument)
    {
        if (argument.Length > 0 && argument.All(c => !char.IsWhiteSpace(c) && c != '"' && c != '\\'))
            return argument;

        var quoted = new StringBuilder("\"");
        var backslashes = 0;
        foreach (var c in argument)
        {
            if (c == '\\')
            {
                backslashes++;
                continue;
            }
            // Backslashes are literal unless they precede a quote
            quoted.Append('\\', c == '"' ? backslashes * 2 + 1 : backslashes);
            backslashes = 0;
            quoted.Append(c);
        }
        // ...or the closing quote
        quoted.Append('\\', backslashes * 2);
        return quoted.Append('"').ToString();
    }
}
