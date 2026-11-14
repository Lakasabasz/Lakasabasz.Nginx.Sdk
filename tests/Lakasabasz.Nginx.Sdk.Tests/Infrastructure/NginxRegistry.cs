using System.Collections.Concurrent;

namespace Lakasabasz.Nginx.Sdk.Tests.Infrastructure;

public static class NginxRegistry
{
    private static readonly TimeSpan AcquireTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RetryInterval = TimeSpan.FromMilliseconds(100);
    private static readonly ConcurrentDictionary<string, Mode> Holders = new();
    private static readonly SemaphoreSlim InitLock = new(1, 1);
    private static bool _initialized;

    public static async Task<IReadOnlyList<UnmetRequirement>> CheckRequirementsAsync()
    {
        var unmet = new List<UnmetRequirement>();
        if (!File.Exists(Consts.Paths.Binary))
            unmet.Add(new(RegistryRequirement.NginxBinary, Consts.Messages.NoBinary(Consts.Paths.Binary)));
        if (!Directory.Exists(Consts.Paths.ConfigRoot))
            unmet.Add(new(RegistryRequirement.ConfigRoot, Consts.Messages.NoConfigRoot(Consts.Paths.ConfigRoot)));
        else
        {
            await using var shared = await LockAsync(Consts.Paths.LockFile, FileShare.Read);
            if (!IsWritable(Consts.Paths.ConfigRoot))
                unmet.Add(new(RegistryRequirement.WritableConfigRoot, Consts.Messages.ConfigRootNotWritable(Consts.Paths.ConfigRoot)));
        }
        var sudo = await NginxProcess.RunAsync(Consts.Paths.Sudo, Consts.Arguments.NoPrompt, Consts.Paths.Binary, Consts.Arguments.Version);
        if (!sudo.Success)
            unmet.Add(new(RegistryRequirement.PasswordlessSudo, Consts.Messages.SudoNotAllowed(Consts.Paths.Binary, sudo)));
        return unmet;
    }

    public static async Task<NginxAccess> AcquireAsync(Mode mode, string holder)
    {
        await InitializeAsync();

        var deadline = DateTime.UtcNow + AcquireTimeout;
        while (true)
        {
            if (TryLock(Consts.Paths.LockFile, mode == Mode.ReadOnly ? FileShare.Read : FileShare.None) is { } stream)
            {
                Holders[holder] = mode;
                return new NginxAccess(stream, mode, holder);
            }

            if (DateTime.UtcNow > deadline)
                throw new TimeoutException(Consts.Messages.AccessTimeout(
                    holder, AcquireTimeout, mode, Holders.Select(h => Consts.Messages.Holder(h.Key, h.Value))));
            await Task.Delay(RetryInterval);
        }
    }

    public static async Task RestoreAsync()
    {
        await SudoAsync(Consts.Paths.Rm, Consts.Arguments.RecursiveForce, Consts.Paths.ConfigRoot);
        await SudoAsync(Consts.Paths.Cp, Consts.Arguments.Archive, Consts.Paths.Backup, Consts.Paths.ConfigRoot);
        await StopAsync();
        await StartAsync();
    }

    internal static void Release(string holder) => Holders.TryRemove(holder, out _);

    private static async Task InitializeAsync()
    {
        if (_initialized)
            return;

        await InitLock.WaitAsync();
        try
        {
            if (_initialized)
                return;

            await using var processLock = await LockAsync(Consts.Paths.InitLockFile, FileShare.None);
            if (!Directory.Exists(Consts.Paths.Backup))
                await RunAsync(Consts.Paths.Cp, Consts.Arguments.Archive, Consts.Paths.ConfigRoot, Consts.Paths.Backup);
            if (!IsRunning())
                await StartAsync();
            _initialized = true;
        }
        finally
        {
            InitLock.Release();
        }
    }

    private static async Task StartAsync()
    {
        await SudoAsync(Consts.Paths.Binary);
        for (var i = 0; i < 50 && !IsRunning(); i++)
            await Task.Delay(100);
        if (!IsRunning())
            throw new InvalidOperationException(Consts.Messages.NginxDidNotStart);
    }

    private static async Task StopAsync()
    {
        if (ReadPid() is not { } pid)
            return;

        await NginxProcess.RunAsync(Consts.Paths.Sudo, Consts.Arguments.NoPrompt, Consts.Paths.Binary, Consts.Arguments.Signal, Consts.Arguments.Quit);
        for (var i = 0; i < 100 && IsRunning(pid); i++)
            await Task.Delay(100);
        if (IsRunning(pid))
            await NginxProcess.RunAsync(Consts.Paths.Sudo, Consts.Arguments.NoPrompt, Consts.Paths.Binary, Consts.Arguments.Signal, Consts.Arguments.Stop);
    }

    private static bool IsRunning() => ReadPid() is { } pid && IsRunning(pid);

    private static bool IsRunning(int pid)
    {
        try
        {
            var stat = File.ReadAllText(Consts.Paths.ProcessStat(pid));
            return stat[(stat.LastIndexOf(Consts.Formats.ProcessNameEnd) + 2)] != Consts.Formats.ZombieState;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static int? ReadPid() =>
        File.Exists(Consts.Paths.PidFile) && int.TryParse(File.ReadAllText(Consts.Paths.PidFile).Trim(), out var pid) ? pid : null;

    private static Task SudoAsync(string command, params string[] arguments) =>
        RunAsync(Consts.Paths.Sudo, [Consts.Arguments.NoPrompt, command, .. arguments]);

    private static async Task RunAsync(string command, params string[] arguments)
    {
        var result = await NginxProcess.RunAsync(command, arguments);
        if (!result.Success)
            throw new InvalidOperationException(Consts.Messages.CommandFailed(command, arguments, result));
    }

    private static async Task<FileStream> LockAsync(string path, FileShare share)
    {
        var deadline = DateTime.UtcNow + AcquireTimeout;
        while (true)
        {
            if (TryLock(path, share) is { } stream)
                return stream;
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException(Consts.Messages.LockTimeout(path, AcquireTimeout));
            await Task.Delay(RetryInterval);
        }
    }

    private static FileStream? TryLock(string path, FileShare share)
    {
        try
        {
            return new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read, share);
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static bool IsWritable(string directory)
    {
        try
        {
            using var probe = new FileStream(
                Path.Combine(directory, Consts.Paths.WriteProbePrefix + Guid.NewGuid().ToString(Consts.Formats.CompactGuid)),
                FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
            return true;
        }
        catch (Exception e) when (e is UnauthorizedAccessException or IOException)
        {
            return false;
        }
    }
}

public sealed class NginxAccess(FileStream lockStream, Mode mode, string holder) : IDisposable
{
    public Mode Mode { get; } = mode;

    public void Dispose()
    {
        NginxRegistry.Release(holder);
        lockStream.Dispose();
    }
}
