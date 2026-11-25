namespace Lakasabasz.Nginx.Sdk;

/// <summary>
/// What a <c>location</c> matches: its modifier and path, read from and written to the directive's arguments.
/// nginx takes the modifier as a word of its own (<c>location = /x</c>) or glued to the path (<c>location =/x</c>);
/// both are read, and it is written as a word of its own.
/// </summary>
internal readonly record struct LocationKey(LocationModifier Modifier, string Path)
{
    private const string Exact = "=";
    private const string PreferPrefix = "^~";
    private const string RegexIgnoreCase = "~*";
    private const string Regex = "~";

    /// <summary>The key of the arguments; <see langword="null"/> when they are not a path with an optional modifier.</summary>
    public static LocationKey? Read(IReadOnlyList<string> arguments) => arguments switch
    {
        [var modifier, var path] when ModifierOf(modifier) is { } known => new LocationKey(known, path),
        [var text] => Glued(text),
        _ => null,
    };

    public string[] Arguments() => Modifier == LocationModifier.None ? [Path] : [TextOf(Modifier), Path];

    private static LocationModifier? ModifierOf(string text) => text switch
    {
        Exact => LocationModifier.Exact,
        PreferPrefix => LocationModifier.PreferPrefix,
        RegexIgnoreCase => LocationModifier.RegexIgnoreCase,
        Regex => LocationModifier.Regex,
        _ => null,
    };

    private static string TextOf(LocationModifier modifier) => modifier switch
    {
        LocationModifier.Exact => Exact,
        LocationModifier.PreferPrefix => PreferPrefix,
        LocationModifier.RegexIgnoreCase => RegexIgnoreCase,
        LocationModifier.Regex => Regex,
        _ => throw new ArgumentOutOfRangeException(nameof(modifier), modifier, "Not a location modifier."),
    };

    /// <summary>
    /// A path with the modifier glued to it, e.g. <c>=/x</c> or <c>~*\.png$</c>. The two-character modifiers are tried
    /// before <c>~</c>, which starts one of them; a modifier alone, without a path, is the path itself.
    /// </summary>
    private static LocationKey Glued(string text)
    {
        foreach (var modifier in new[] { PreferPrefix, RegexIgnoreCase, Exact, Regex })
        {
            if (text.Length > modifier.Length && text.StartsWith(modifier, StringComparison.Ordinal))
                return new LocationKey(ModifierOf(modifier)!.Value, text.Substring(modifier.Length));
        }
        return new LocationKey(LocationModifier.None, text);
    }
}
