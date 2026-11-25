namespace Lakasabasz.Nginx.Sdk.Internal.Syntax;

/// <summary>What a character means in nginx's config syntax, where it is read as a token boundary.</summary>
internal static class NginxCharacters
{
    /// <summary><c>{</c>: opens a block, and ends the words of a block directive.</summary>
    public static bool IsBlockBegin(this char c) => c == '{';

    /// <summary><c>}</c>: closes a block.</summary>
    public static bool IsBlockEnd(this char c) => c == '}';

    /// <summary><c>;</c>: ends a simple directive.</summary>
    public static bool IsDirectiveEnd(this char c) => c == ';';

    /// <summary><c>#</c>: starts a comment, at the start of a token.</summary>
    public static bool IsCommentStart(this char c) => c == '#';

    /// <summary>A double or single quote: starts a quoted word.</summary>
    public static bool IsQuote(this char c) => c is '"' or '\'';

    /// <summary><c>\</c>: escapes the character after it.</summary>
    public static bool IsEscape(this char c) => c == '\\';

    /// <summary>Whitespace between tokens: space, tab, <c>\r</c>, <c>\n</c>.</summary>
    public static bool IsSpace(this char c) => c is ' ' or '\t' or '\r' or '\n';
}
