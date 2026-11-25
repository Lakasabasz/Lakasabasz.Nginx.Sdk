namespace Lakasabasz.Nginx.Sdk;

/// <summary>How a <c>location</c> matches the request path; without a modifier it is a prefix.</summary>
public enum LocationModifier
{
    /// <summary>A prefix (no modifier): <c>location /api</c>.</summary>
    None,

    /// <summary>Exactly this path: <c>location = /health</c>.</summary>
    Exact,

    /// <summary>A prefix that, when it is the longest match, stops the search for regexes: <c>location ^~ /static/</c>.</summary>
    PreferPrefix,

    /// <summary>A case-sensitive regex: <c>location ~ \.php$</c>.</summary>
    Regex,

    /// <summary>A case-insensitive regex: <c>location ~* \.(png|jpg)$</c>.</summary>
    RegexIgnoreCase,
}
