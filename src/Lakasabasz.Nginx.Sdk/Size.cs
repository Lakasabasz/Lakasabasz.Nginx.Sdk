using System.Globalization;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>A size as nginx writes it: bytes, or a number with <c>k</c>, <c>m</c> or <c>g</c>, e.g. <c>50m</c>.</summary>
public readonly record struct Size
{
    private const long Kilobyte = 1024;
    private const long Megabyte = 1024 * Kilobyte;
    private const long Gigabyte = 1024 * Megabyte;

    /// <summary>The units, largest first: a size is written in the first one that holds it exactly.</summary>
    private static readonly (char Unit, long Length)[] LargestFirst = [('g', Gigabyte), ('m', Megabyte), ('k', Kilobyte)];

    private Size(long bytes)
    {
        Bytes = bytes;
    }

    /// <summary>The size in bytes.</summary>
    public long Bytes { get; }

    /// <summary>A size of <paramref name="bytes"/> bytes.</summary>
    public static Size FromBytes(long bytes) => new(bytes);

    /// <summary>A size in kilobytes (1024 bytes), written <c>…k</c>.</summary>
    public static Size FromKilobytes(long kilobytes) => new(checked(kilobytes * Kilobyte));

    /// <summary>A size in megabytes (1024 kilobytes), written <c>…m</c>.</summary>
    public static Size FromMegabytes(long megabytes) => new(checked(megabytes * Megabyte));

    /// <summary>A size in gigabytes (1024 megabytes), written <c>…g</c>.</summary>
    public static Size FromGigabytes(long gigabytes) => new(checked(gigabytes * Gigabyte));

    /// <summary>The size written in nginx's way, e.g. <c>512</c>, <c>8k</c> or <c>50m</c> (units in either case).</summary>
    /// <exception cref="FormatException">The text is not a size.</exception>
    public static Size Parse(string text) =>
        TryParse(text, out var size) ? size : throw new FormatException($"\"{text}\" is not a size: a number, optionally with k, m or g.");

    /// <summary>The size written in nginx's way; <see langword="false"/> when the text is not one or is too large.</summary>
    public static bool TryParse(string text, out Size size)
    {
        size = default;
        if (string.IsNullOrEmpty(text))
            return false;
        var unit = LengthOf(text[^1]);
        var digits = unit is null ? text : text.Substring(0, text.Length - 1);
        if (!long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number > long.MaxValue / (unit ?? 1))
            return false;
        size = new Size(number * (unit ?? 1));
        return true;
    }

    /// <summary>The size in the largest unit that holds it exactly, e.g. <c>50m</c>; bytes otherwise, also for 0.</summary>
    public override string ToString()
    {
        if (Bytes == 0)
            return Bytes.ToString(CultureInfo.InvariantCulture);
        foreach (var (unit, length) in LargestFirst)
        {
            if (Bytes % length == 0)
                return (Bytes / length).ToString(CultureInfo.InvariantCulture) + unit;
        }
        return Bytes.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>The bytes in the unit written as <paramref name="unit"/> (<c>k</c>, <c>m</c>, <c>g</c>, either case); <see langword="null"/> for a digit or other character.</summary>
    private static long? LengthOf(char unit) => char.ToLowerInvariant(unit) switch
    {
        'k' => Kilobyte,
        'm' => Megabyte,
        'g' => Gigabyte,
        _ => null,
    };
}
