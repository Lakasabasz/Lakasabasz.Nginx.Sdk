using Lakasabasz.Nginx.Sdk.Internal.Structure;

namespace Lakasabasz.Nginx.Sdk.Views;

/// <summary>Reads a typed value from the text of a directive's argument.</summary>
internal delegate bool TryParse<T>(string text, out T value);

/// <summary>
/// One-value directives of a block read and written as typed values: the first directive of the name is read; writing
/// leaves one directive with the value, and <see langword="null"/> removes them all.
/// </summary>
internal static class DirectiveValues
{
    public static string? GetText(StructureBlock block, string name) => First(block, name) switch
    {
        null => null,
        { Arguments.Count: 1 } directive => directive.Arguments[0],
        var directive => throw NotA(directive, "one value"),
    };

    public static T? Get<T>(StructureBlock block, string name, string what, TryParse<T> parse)
        where T : struct
    {
        if (First(block, name) is not { } directive)
            return null;
        if (directive.Arguments.Count == 1 && parse(directive.Arguments[0], out var value))
            return value;
        throw NotA(directive, what);
    }

    public static bool? GetSwitch(StructureBlock block, string name) => Get<bool>(block, name, "on or off", TryParseSwitch);

    public static int? GetNumber(StructureBlock block, string name) => Get<int>(block, name, "a number", TryParseNumber);

    public static Size? GetSize(StructureBlock block, string name) => Get<Size>(block, name, "a size", Size.TryParse);

    public static TimeSpan? GetTime(StructureBlock block, string name) => Get<TimeSpan>(block, name, "a time", NginxTime.TryParse);

    public static void SetText(StructureBlock block, string name, string? value)
    {
        if (value is null)
            block.Remove(name).OrThrow();
        else
            block.Set(name, [value]).OrThrow();
    }

    public static void SetSwitch(StructureBlock block, string name, bool? value) => SetText(block, name, value switch
    {
        null => null,
        true => "on",
        false => "off",
    });

    public static void SetNumber(StructureBlock block, string name, int? value) =>
        SetText(block, name, value?.ToString(System.Globalization.CultureInfo.InvariantCulture));

    public static void SetSize(StructureBlock block, string name, Size? value) => SetText(block, name, value?.ToString());

    public static void SetTime(StructureBlock block, string name, TimeSpan? value) =>
        SetText(block, name, value is { } time ? NginxTime.Format(time) : null);

    public static bool TryParseSwitch(string text, out bool value)
    {
        value = text == "on";
        return text is "on" or "off";
    }

    public static bool TryParseNumber(string text, out int value) =>
        int.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out value);

    /// <summary>A directive whose arguments a typed property cannot read, e.g. a variable where a size is expected.</summary>
    public static InvalidOperationException NotA(StructureDirective directive, string what)
    {
        var place = directive.Line > 0 ? $" at line {directive.Line}" : string.Empty;
        return new InvalidOperationException(
            $"{directive.Name}{place} is \"{string.Join(" ", directive.Arguments)}\", not {what}: read it through Directives.");
    }

    private static StructureDirective? First(StructureBlock block, string name) =>
        block.Directives.FirstOrDefault(directive => directive.Name == name);
}
