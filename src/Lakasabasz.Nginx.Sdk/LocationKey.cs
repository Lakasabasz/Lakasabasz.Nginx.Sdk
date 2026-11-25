namespace Lakasabasz.Nginx.Sdk;

/// <summary>
/// What a <c>location</c> matches: its modifier and path, read from and written to the directive's arguments.
/// nginx takes the modifier as a word of its own (<c>location = /x</c>) or glued to the path (<c>location =/x</c>);
/// both are read, and it is written as a word of its own.
/// </summary>
internal readonly record struct LocationKey(LocationModifier Modifier, string Path)
{
    private const string ExactText = "=";
    private const string PreferPrefixText = "^~";
    private const string RegexIgnoreCaseText = "~*";
    private const string RegexText = "~";

    /// <summary>
    /// The modifiers in the order a path with a modifier glued to it is tried: <c>~</c> last, as it starts <c>~*</c>.
    /// </summary>
    private static readonly (string Text, LocationModifier Modifier)[] GluedOrder =
    [
        (PreferPrefixText, LocationModifier.PreferPrefix),
        (RegexIgnoreCaseText, LocationModifier.RegexIgnoreCase),
        (ExactText, LocationModifier.Exact),
        (RegexText, LocationModifier.Regex),
    ];

    /// <summary>
    /// The key of the arguments: a modifier and a path, or one word (a path, perhaps with a modifier glued to it);
    /// <see langword="null"/> for anything else, e.g. two words of which the first is no modifier.
    /// </summary>
    public static LocationKey? Read(IReadOnlyList<string> arguments) => arguments switch
    {
        [var modifierText, var path] when ModifierOf(modifierText) is { } modifier => new LocationKey(modifier, path),
        [var text] => Glued(text),
        _ => null,
    };

    /// <summary>The arguments of the <c>location</c> directive: the modifier as a word of its own (none for a prefix), then the path.</summary>
    public string[] ToArguments() => Modifier == LocationModifier.None ? [Path] : [TextOf(Modifier), Path];

    private static LocationModifier? ModifierOf(string text) =>
        GluedOrder.Where(known => known.Text == text).Select(known => (LocationModifier?)known.Modifier).FirstOrDefault();

    private static string TextOf(LocationModifier modifier) => modifier switch
    {
        LocationModifier.Exact => ExactText,
        LocationModifier.PreferPrefix => PreferPrefixText,
        LocationModifier.RegexIgnoreCase => RegexIgnoreCaseText,
        LocationModifier.Regex => RegexText,
        _ => throw new ArgumentOutOfRangeException(nameof(modifier), modifier, "Not a location modifier."),
    };

    /// <summary>A path with the modifier glued to it, e.g. <c>=/x</c> or <c>~*\.png$</c>; a modifier alone, without a path, is the path itself.</summary>
    private static LocationKey Glued(string text)
    {
        foreach (var (modifierText, modifier) in GluedOrder)
        {
            if (text.Length > modifierText.Length && text.StartsWith(modifierText, StringComparison.Ordinal))
                return new LocationKey(modifier, text.Substring(modifierText.Length));
        }
        return new LocationKey(LocationModifier.None, text);
    }
}
