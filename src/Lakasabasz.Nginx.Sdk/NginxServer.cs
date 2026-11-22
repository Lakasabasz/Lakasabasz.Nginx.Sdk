namespace Lakasabasz.Nginx.Sdk;

/// <summary>The nginx installation: its environment, version and commands acting on the files on disk.</summary>
public sealed class NginxServer
{
    internal NginxServer(NginxEnvironment environment)
    {
        Environment = environment;
    }

    /// <summary>Paths of the installation and whether commands use sudo.</summary>
    public NginxEnvironment Environment { get; }

    /// <summary>Version of the nginx binary, e.g. 1.28.3.</summary>
    /// <exception cref="NotImplementedException">Not implemented yet.</exception>
    public Version Version => throw new NotImplementedException();

    /// <summary>
    /// Tests the configuration files on disk (<c>nginx -t</c>). Does not throw when the configuration is invalid.
    /// </summary>
    /// <exception cref="NotImplementedException">Not implemented yet.</exception>
    public NginxTestResult Test() => throw new NotImplementedException();

    /// <summary>Sends nginx the reload signal (<c>nginx -s reload</c>).</summary>
    /// <exception cref="NotImplementedException">Not implemented yet.</exception>
    public void Reload() => throw new NotImplementedException();
}
