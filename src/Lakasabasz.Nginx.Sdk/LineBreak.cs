namespace Lakasabasz.Nginx.Sdk;

/// <summary>The line break of a file's new and changed text.</summary>
public enum LineBreak
{
    /// <summary><c>\n</c>, as on Linux.</summary>
    Lf,

    /// <summary><c>\r\n</c>, as on Windows.</summary>
    CrLf,
}
