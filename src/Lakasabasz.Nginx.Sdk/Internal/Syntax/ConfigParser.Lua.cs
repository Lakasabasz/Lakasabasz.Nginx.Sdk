namespace Lakasabasz.Nginx.Sdk.Internal.Syntax;

/// <summary>Raw blocks: the Lua code of OpenResty's <c>*_by_lua_block</c>, kept as text.</summary>
internal sealed partial class ConfigParser
{
    private const string LuaBlockSuffix = "_by_lua_block";

    private static bool IsRawBlock(string name) => name.EndsWith(LuaBlockSuffix, StringComparison.Ordinal);

    /// <summary>The body of a raw block, up to its closing <c>}</c> (consumed); braces in Lua strings and comments do not count.</summary>
    private string ReadRawBody()
    {
        var start = _text.Position;
        var depth = 0;
        while (!_text.AtEnd)
        {
            if (SkipLuaString() || SkipLuaComment() || SkipLuaLongBracket())
                continue;

            switch (_text.Current)
            {
                case '{':
                    depth++;
                    break;
                case '}' when depth == 0:
                    var body = _text.Take(start);
                    _text.Advance();
                    return body;
                case '}':
                    depth--;
                    break;
            }
            _text.Advance();
        }
        throw new SyntaxError(_text.Position, EndOfFileInBlock);
    }

    /// <summary>Skips a Lua string in quotes, which ends at its quote or at the end of the line.</summary>
    private bool SkipLuaString()
    {
        var quote = _text.Current;
        if (quote is not ('"' or '\''))
            return false;
        _text.Advance();
        while (!_text.AtEnd && _text.Current != quote && _text.Current != '\n')
            _text.Advance(_text.Current == Escape ? 2 : 1);
        _text.Advance();
        return true;
    }

    /// <summary>Skips a Lua comment: <c>--</c> to the end of the line, or <c>--[[ … ]]</c>.</summary>
    private bool SkipLuaComment()
    {
        if (_text.Current != '-' || _text.Peek(1) != '-')
            return false;
        _text.Advance(2);
        if (!SkipLuaLongBracket())
            SkipToLineEnd();
        return true;
    }

    /// <summary>Skips a Lua long bracket (<c>[[ … ]]</c>, <c>[==[ … ]==]</c>) starting here, if there is one.</summary>
    private bool SkipLuaLongBracket()
    {
        if (_text.Current != '[')
            return false;
        var level = 0;
        while (_text.Peek(1 + level) == '=')
            level++;
        if (_text.Peek(1 + level) != '[')
            return false;

        var close = "]" + new string('=', level) + "]";
        var end = _text.Text.IndexOf(close, _text.Position + level + 2, StringComparison.Ordinal);
        _text.MoveTo(end < 0 ? _text.Text.Length : end + close.Length);
        return true;
    }
}
