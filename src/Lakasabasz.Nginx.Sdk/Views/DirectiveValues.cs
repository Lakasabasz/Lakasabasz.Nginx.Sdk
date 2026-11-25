using System.Globalization;
using Lakasabasz.Nginx.Sdk.Internal.Structure;

namespace Lakasabasz.Nginx.Sdk.Views;

/// <summary>Reads a typed value from the text of a directive's argument.</summary>
internal delegate bool TryParse<T>(string text, out T value);

/// <summary>A kind of value a directive's argument holds: how it is read, written and named in an error.</summary>
/// <param name="Description">What the value is, for the error when it cannot be read, e.g. <c>on or off</c>.</param>
/// <param name="TryParse">Reads the value from the argument's text.</param>
/// <param name="Write">The argument's text of a value.</param>
internal sealed record ValueKind<T>(string Description, TryParse<T> TryParse, Func<T, string> Write);

/// <summary>
/// One-value directives of a block read and written as typed values: the first directive of the name is read; writing
/// leaves one directive with the value, and <see langword="null"/> removes them all.
/// </summary>
internal static class DirectiveValues
{
    private const string On = "on";
    private const string Off = "off";

    public static readonly ValueKind<string> Text = new("one value", TryParseText, text => text);

    public static readonly ValueKind<bool> Switch = new("on or off", TryParseSwitch, value => value ? On : Off);

    public static readonly ValueKind<int> Number = new("a number", TryParseNumber, value => value.ToString(CultureInfo.InvariantCulture));

    public static readonly ValueKind<Size> Size = new("a size", Sdk.Size.TryParse, size => size.ToString());

    public static readonly ValueKind<TimeSpan> Time = new("a time", NginxTime.TryParse, NginxTime.Format);

    public static string? GetText(StructureBlock block, string name) =>
        First(block, name) is { } directive ? Read(directive, Text) : null;

    public static T? Get<T>(StructureBlock block, string name, ValueKind<T> kind)
        where T : struct => First(block, name) is { } directive ? Read(directive, kind) : null;

    public static void SetText(StructureBlock block, string name, string? value)
    {
        if (value is null)
            block.Remove(name).OrThrow();
        else
            block.Set(name, [value]).OrThrow();
    }

    public static void Set<T>(StructureBlock block, string name, ValueKind<T> kind, T? value)
        where T : struct => SetText(block, name, value is { } set ? kind.Write(set) : null);

    /// <summary>The error for a directive whose arguments a typed property cannot read, e.g. a variable where a size is expected.</summary>
    public static InvalidOperationException Unreadable(StructureDirective directive, string what)
    {
        var place = directive.Line > 0 ? $" at line {directive.Line}" : string.Empty;
        return new InvalidOperationException(
            $"{directive.Name}{place} is \"{string.Join(" ", directive.Arguments)}\", not {what}: read it through Directives.");
    }

    private static T Read<T>(StructureDirective directive, ValueKind<T> kind) =>
        directive.Arguments is [var text] && kind.TryParse(text, out var value) ? value : throw Unreadable(directive, kind.Description);

    private static bool TryParseText(string text, out string value)
    {
        value = text;
        return true;
    }

    private static bool TryParseSwitch(string text, out bool value)
    {
        switch (text)
        {
            case On:
                value = true;
                return true;
            case Off:
                value = false;
                return true;
            default:
                value = false;
                return false;
        }
    }

    private static bool TryParseNumber(string text, out int value) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);

    private static StructureDirective? First(StructureBlock block, string name) =>
        block.Directives.FirstOrDefault(directive => directive.Name == name);
}
