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
    public static string Of(string path)
    {
        var full = Path.GetFullPath(path);
        try
        {
            var resolved = NativeMethods.RealPath(full, IntPtr.Zero);
            if (resolved == IntPtr.Zero)
                return full;
            try
            {
                return Marshal.PtrToStringAnsi(resolved) ?? full;
            }
            finally
            {
                NativeMethods.Free(resolved);
            }
        }
        catch (DllNotFoundException)
        {
            return full;
        }
        catch (EntryPointNotFoundException)
        {
            return full;
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
