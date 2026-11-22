// The nginx installation: its version, a config test and a reload.
// nginx needs root, so the package runs it through sudo -n: the user needs a passwordless sudo rule for the binary.
using Lakasabasz.Nginx.Sdk;

var nginx = Nginx.Init(sudo: true);
Console.WriteLine($"nginx {nginx.Server.Version} ({nginx.Server.Environment.Binary})");

// nginx -t on the files on disk: an invalid config is a result, not an exception
var result = nginx.Server.Test();
Console.WriteLine(result.Success ? "Config accepted" : "Config rejected");
foreach (var error in result.Errors)
    Console.WriteLine($"  [{error.Level}] {error.Message} ({error.File}:{error.Line})");
foreach (var warning in result.Warnings)
    Console.WriteLine($"  [{warning.Level}] {warning.Message}");

if (!result.Success)
    return 1;

try
{
    nginx.Server.Reload();
    Console.WriteLine("Reloaded");
}
catch (NginxCommandException e)
{
    // e.g. nginx is not running, or sudo asks for a password
    Console.WriteLine($"Reload failed: {e.Command} exited with {e.ExitCode}: {e.Output.Trim()}");
    return 1;
}

return 0;
