using System.Runtime.InteropServices;

namespace Lakasabasz.Nginx.Sdk.Internal;

/// <summary>
/// Resolves every symbolic link in a path (realpath(3)), so a file reached through a link, such as an enabled site in
/// <c>sites-enabled</c>, is the same file as its target. netstandard2.0 has no API for links, hence libc.
/// </summary>
internal static class RealPath
{
    /// <summary>
    /// The absolute path with every link resolved; the absolute path as given when it does not exist or links cannot
    /// be resolved on this platform.
    /// </summary>
    public static string Of(string path) => TryResolve(path, out var real) ? real : Path.GetFullPath(path);

    /// <summary>
    /// Whether the path leads to an existing file or directory, following every link; a link to a missing file (a
    /// dangling link) does not. Where links cannot be resolved (no libc), whether the path exists.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="realPath">The absolute path with every link resolved.</param>
    public static bool TryResolve(string path, out string realPath)
    {
        realPath = Path.GetFullPath(path);
        try
        {
            var resolved = NativeMethods.RealPath(realPath, IntPtr.Zero);
            if (resolved == IntPtr.Zero)
                return false;
            try
            {
                realPath = Marshal.PtrToStringAnsi(resolved) ?? realPath;
                return true;
            }
            finally
            {
                NativeMethods.Free(resolved);
            }
        }
        catch (DllNotFoundException)
        {
            return File.Exists(realPath) || Directory.Exists(realPath);
        }
        catch (EntryPointNotFoundException)
        {
            return File.Exists(realPath) || Directory.Exists(realPath);
        }
    }

    private static class NativeMethods
    {
        [DllImport("libc", EntryPoint = "realpath", SetLastError = true, CharSet = CharSet.Ansi, BestFitMapping = false,
            ThrowOnUnmappableChar = true)]
        public static extern IntPtr RealPath([MarshalAs(UnmanagedType.LPStr)] string path, IntPtr resolvedPath);

        [DllImport("libc", EntryPoint = "free")]
        public static extern void Free(IntPtr pointer);
    }
}
