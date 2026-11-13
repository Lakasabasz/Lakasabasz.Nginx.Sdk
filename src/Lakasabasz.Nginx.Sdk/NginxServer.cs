using Lakasabasz.Nginx.Sdk.Internal;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>The nginx installation: its environment, version and commands acting on the files on disk.</summary>
public sealed class NginxServer
{
    private readonly Lazy<Version> _version;

    internal NginxServer(NginxEnvironment environment)
    {
        Environment = environment;
        _version = new Lazy<Version>(ReadVersion);
    }

    /// <summary>Paths of the installation and whether commands use sudo.</summary>
    public NginxEnvironment Environment { get; }

    /// <summary>Version of the nginx binary, e.g. 1.28.3.</summary>
    /// <exception cref="NginxCommandException">The binary cannot be run or reports no version.</exception>
    public Version Version => _version.Value;

    /// <summary>
    /// Tests the configuration files on disk (<c>nginx -t</c>). Does not throw when the configuration is invalid.
    /// </summary>
    /// <exception cref="NginxCommandException">nginx cannot be run.</exception>
    public NginxTestResult Test()
    {
        var result = NginxCommand.Run(Environment, "-t");
        return NginxOutputParser.ParseTestResult(result.ExitCode, result.StandardError);
    }

    /// <summary>Sends nginx the reload signal (<c>nginx -s reload</c>).</summary>
    /// <exception cref="NginxCommandException">nginx cannot be run or rejected the signal.</exception>
    public void Reload()
    {
        // TODO Etap 7: confirm that nginx applied the configuration (NginxReloadException);
        // the exit code of -s reload is 0 even when the new configuration fails to bind a port.
        var result = NginxCommand.Run(Environment, "-s", "reload");
        if (result.ExitCode != 0)
            throw new NginxCommandException($"nginx reload failed: {result.StandardError.Trim()}", result.Command, result.ExitCode, result.Output);
    }

    private Version ReadVersion()
    {
        var result = NginxCommand.Run(Environment, "-v");
        return NginxOutputParser.ParseVersion(result.StandardError)
            ?? throw new NginxCommandException($"nginx reported no version: {result.Output.Trim()}", result.Command, result.ExitCode, result.Output);
    }
}
