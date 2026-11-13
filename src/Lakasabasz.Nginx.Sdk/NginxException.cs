namespace Lakasabasz.Nginx.Sdk;

/// <summary>Base class of every exception thrown by this package.</summary>
public abstract class NginxException : Exception
{
    /// <summary>Creates the exception.</summary>
    protected NginxException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// An nginx command could not be run or failed to execute: missing binary, sudo needing a password,
/// timeout, or a non-zero exit code of a command that has no more specific exception.
/// </summary>
public sealed class NginxCommandException : NginxException
{
    /// <summary>Creates the exception.</summary>
    public NginxCommandException(string message, string command, int? exitCode, string output, Exception? innerException = null)
        : base(message, innerException)
    {
        Command = command;
        ExitCode = exitCode;
        Output = output;
    }

    /// <summary>The command line that was run.</summary>
    public string Command { get; }

    /// <summary>Exit code, or <see langword="null"/> when the command did not finish.</summary>
    public int? ExitCode { get; }

    /// <summary>Combined standard output and error of the command.</summary>
    public string Output { get; }
}
