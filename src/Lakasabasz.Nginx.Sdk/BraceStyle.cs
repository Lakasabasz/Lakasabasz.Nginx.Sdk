namespace Lakasabasz.Nginx.Sdk;

/// <summary>How a block is written.</summary>
public enum BraceStyle
{
    /// <summary><c>{</c> at the end of the block's line, each directive on a line of its own: <c>server {</c>.</summary>
    EndOfLine,

    /// <summary><c>{</c> on a line of its own, under the block's name.</summary>
    NewLine,

    /// <summary>The whole block on one line: <c>location /health { return 204; }</c>. A block holding a block or a comment cannot be.</summary>
    OneLine,
}
