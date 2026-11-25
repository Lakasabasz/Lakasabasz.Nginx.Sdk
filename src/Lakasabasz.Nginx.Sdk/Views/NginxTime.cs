using System.Globalization;
using System.Text;

namespace Lakasabasz.Nginx.Sdk.Views;

/// <summary>
/// Times as nginx writes them: numbers with units (<c>ms</c>, <c>s</c>, <c>m</c>, <c>h</c>, <c>d</c>, <c>w</c>,
/// <c>M</c>, <c>y</c>), e.g. <c>90s</c> or <c>1h30m</c>; a number without a unit is in seconds.
/// </summary>
internal static class NginxTime
{
    private static readonly (string Unit, TimeSpan Length)[] Units =
    [
        ("y", TimeSpan.FromDays(365)),
        ("M", TimeSpan.FromDays(30)),
        ("w", TimeSpan.FromDays(7)),
        ("d", TimeSpan.FromDays(1)),
        ("h", TimeSpan.FromHours(1)),
        ("m", TimeSpan.FromMinutes(1)),
        ("s", TimeSpan.FromSeconds(1)),
        ("ms", TimeSpan.FromMilliseconds(1)),
    ];

    public static bool TryParse(string text, out TimeSpan value)
    {
        value = TimeSpan.Zero;
        var position = 0;
        while (position < text.Length)
        {
            if (text[position] == ' ')
            {
                position++;
                continue;
            }

            var start = position;
            while (position < text.Length && char.IsDigit(text[position]))
                position++;
            if (position == start)
                return false;
            var unit = UnitAt(text, position);
            if (LengthOf(unit) is not { } length
                || !long.TryParse(text.Substring(start, position - start), NumberStyles.None, CultureInfo.InvariantCulture, out var number))
                return false;
            position += unit.Length;
            value += TimeSpan.FromTicks(number * length.Ticks);
        }
        return text.Length > 0;
    }

    /// <summary>The shortest text: the largest units first, e.g. 90 minutes is <c>1h30m</c>.</summary>
    public static string Format(TimeSpan time)
    {
        if (time <= TimeSpan.Zero)
            return "0s";
        var text = new StringBuilder();
        var rest = time;
        foreach (var (unit, length) in Units.Where(unit => unit.Unit is not ("y" or "M" or "w")))
        {
            var count = rest.Ticks / length.Ticks;
            if (count == 0)
                continue;
            text.Append(count.ToString(CultureInfo.InvariantCulture)).Append(unit);
            rest -= TimeSpan.FromTicks(count * length.Ticks);
        }
        return text.Length > 0 ? text.ToString() : "0s";
    }

    private static string UnitAt(string text, int position)
    {
        if (position >= text.Length || text[position] == ' ')
            return string.Empty;
        if (string.CompareOrdinal(text, position, "ms", 0, 2) == 0)
            return "ms";
        return text[position].ToString();
    }

    /// <summary>The length of a unit; <see langword="null"/> for a text that is not one.</summary>
    private static TimeSpan? LengthOf(string unit) => unit.Length == 0
        ? TimeSpan.FromSeconds(1)
        : Units.Where(known => known.Unit == unit).Select(known => (TimeSpan?)known.Length).FirstOrDefault();
}
