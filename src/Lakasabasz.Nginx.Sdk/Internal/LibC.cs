using System.Runtime.InteropServices;

namespace Lakasabasz.Nginx.Sdk.Internal;

/// <summary>
/// The C library functions nginx itself uses for paths, so the package reads paths exactly as nginx does:
/// realpath(3) for links, glob(3) for <c>include</c> patterns and fnmatch(3) for matching names. netstandard2.0 has
/// none of them.
/// </summary>
internal static class LibC
{
    private const string Library = "libc";

    private const int GlobNoMatch = 3;
    private const int FnmPathname = 1;
    private const int FnmPeriod = 4;

    /// <summary>
    /// Large enough for glob_t of glibc (72 bytes) and musl (88 bytes) on 64-bit; only its first two fields are read:
    /// <c>size_t gl_pathc</c> and <c>char **gl_pathv</c>, in this order in both.
    /// </summary>
    private const int GlobSize = 256;

    /// <summary>
    /// Whether the path leads to an existing file or directory, following every link; a link to a missing file (a
    /// dangling link) does not. Where links cannot be resolved (no libc), whether the path exists.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="realPath">The absolute path with every link resolved.</param>
    public static bool TryRealPath(string path, out string realPath)
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
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            return File.Exists(realPath) || Directory.Exists(realPath);
        }
    }

    /// <summary>
    /// The paths matching a pattern, as nginx expands an <c>include</c>: glob(3) without flags, so sorted, with hidden
    /// files matched only by a pattern starting with a dot, and directories and dangling links included.
    /// </summary>
    /// <param name="pattern">An absolute pattern.</param>
    /// <returns>The matching paths; empty when none match, which nginx accepts.</returns>
    /// <exception cref="IOException">glob(3) failed, e.g. a directory could not be read.</exception>
    public static IReadOnlyList<string> Glob(string pattern)
    {
        var glob = Marshal.AllocHGlobal(GlobSize);
        try
        {
            for (var i = 0; i < GlobSize; i++)
                Marshal.WriteByte(glob, i, 0);

            var result = Native(() => NativeMethods.Glob(pattern, 0, IntPtr.Zero, glob));
            if (result == GlobNoMatch)
                return [];
            if (result != 0)
                throw new IOException($"glob() \"{pattern}\" failed ({result}).");

            var count = Marshal.ReadIntPtr(glob).ToInt64();
            var paths = Marshal.ReadIntPtr(glob, IntPtr.Size);
            var matches = new List<string>((int)count);
            for (var i = 0; i < count; i++)
                matches.Add(Marshal.PtrToStringAnsi(Marshal.ReadIntPtr(paths, i * IntPtr.Size))!);
            return matches;
        }
        finally
        {
            Native(() => NativeMethods.GlobFree(glob));
            Marshal.FreeHGlobal(glob);
        }
    }

    /// <summary>
    /// Whether the text matches a glob pattern as glob(3) matches a path: <c>*</c>, <c>?</c> and <c>[…]</c> do not match
    /// <c>/</c>, and a leading dot of a name is matched only by a dot.
    /// </summary>
    public static bool FnMatch(string pattern, string text) =>
        Native(() => NativeMethods.FnMatch(pattern, text, FnmPathname | FnmPeriod)) == 0;

    private static T Native<T>(Func<T> call)
    {
        try
        {
            return call();
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            throw new PlatformNotSupportedException("Glob patterns need the C library (Linux), as nginx does.", e);
        }
    }

    private static void Native(Action call) => Native(() =>
    {
        call();
        return 0;
    });

    private static class NativeMethods
    {
        [DllImport(Library, EntryPoint = "realpath", SetLastError = true, CharSet = CharSet.Ansi, BestFitMapping = false,
            ThrowOnUnmappableChar = true)]
        public static extern IntPtr RealPath([MarshalAs(UnmanagedType.LPStr)] string path, IntPtr resolvedPath);

        [DllImport(Library, EntryPoint = "free")]
        public static extern void Free(IntPtr pointer);

        [DllImport(Library, EntryPoint = "glob", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int Glob([MarshalAs(UnmanagedType.LPStr)] string pattern, int flags, IntPtr errorFunction, IntPtr glob);

        [DllImport(Library, EntryPoint = "globfree")]
        public static extern void GlobFree(IntPtr glob);

        [DllImport(Library, EntryPoint = "fnmatch", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int FnMatch([MarshalAs(UnmanagedType.LPStr)] string pattern, [MarshalAs(UnmanagedType.LPStr)] string text, int flags);
    }
}
