namespace Lakasabasz.Nginx.Sdk.Internal.Syntax;

/// <summary>Raw blocks: the Lua code of OpenResty's <c>*_by_lua_block</c>, kept as text.</summary>
internal static partial class ConfigParser
{
    private const string LuaBlockSuffix = "_by_lua_block";

    private static bool IsRawBlock(string name) => name.EndsWith(LuaBlockSuffix, StringComparison.Ordinal);

    /// <summary>The body of a raw block, up to its closing <c>}</c> (consumed); braces in Lua strings and comments do not count.</summary>
    private static Result<string, SyntaxProblem> TakeRawBody(TextCursor cursor)
    {
        var start = cursor.CurrentPosition;
        var depth = 0;
        while (!cursor.AtEnd)
        {
            if (SkipLuaString(cursor) || SkipLuaComment(cursor) || SkipLuaLongBracket(cursor))
                continue;

            switch (cursor.Current)
            {
                case '{':
                    depth++;
                    break;
                case '}' when depth == 0:
                    var body = cursor.ReadSince(start);
                    cursor.Advance();
                    return Success(body);
                case '}':
                    depth--;
                    break;
            }
            cursor.Advance();
        }
        return Failure<string>(Problem(cursor, EndOfFileInBlock));
    }

    /// <summary>Skips a Lua string in quotes, which ends at its quote or at the end of the line.</summary>
    private static bool SkipLuaString(TextCursor cursor)
    {
        var quote = cursor.Current;
        if (!quote.IsQuote())
            return false;
        cursor.Advance();
        while (!cursor.AtEnd && cursor.Current != quote && cursor.Current != '\n')
            cursor.Advance(cursor.Current.IsEscape() ? 2 : 1);
        cursor.Advance();
        return true;
    }

    /// <summary>Skips a Lua comment: <c>--</c> to the end of the line, or <c>--[[ … ]]</c>.</summary>
    private static bool SkipLuaComment(TextCursor cursor)
    {
        if (cursor.Current != '-' || cursor.Peek(1) != '-')
            return false;
        cursor.Advance(2);
        if (!SkipLuaLongBracket(cursor))
            SkipToLineEnd(cursor);
        return true;
    }

    /// <summary>Skips a Lua long bracket (<c>[[ … ]]</c>, <c>[==[ … ]==]</c>) starting here, if there is one.</summary>
    private static bool SkipLuaLongBracket(TextCursor cursor)
    {
        if (cursor.Current != '[')
            return false;
        var level = 0;
        while (cursor.Peek(1 + level) == '=')
            level++;
        if (cursor.Peek(1 + level) != '[')
            return false;

        var close = "]" + new string('=', level) + "]";
        var end = cursor.Text.IndexOf(close, cursor.Index + level + 2, StringComparison.Ordinal);
        cursor.MoveTo(end < 0 ? cursor.Text.Length : end + close.Length);
        return true;
    }
}
