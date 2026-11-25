using System.Globalization;
using System.Text;

namespace Lakasabasz.Nginx.Sdk.Views;

/// <summary>
/// Times as nginx writes them: parts of a number and a unit (<c>ms</c>, <c>s</c>, <c>m</c>, <c>h</c>, <c>d</c>,
/// <c>w</c>, <c>M</c>, <c>y</c>), e.g. <c>90s</c> or <c>1h 30m</c>; a number without a unit is in seconds.
/// </summary>
internal static class NginxTime
{
    private const string Milliseconds = "ms";
    private const string Seconds = "s";

    /// <summary>
    /// The units written, largest first. Weeks, months (30 days) and years (365 days) are read but not written: a
    /// time is written in days at most, so it reads back the same.
    /// </summary>
    private static readonly (string Unit, TimeSpan Length)[] Written =
    [
        ("d", TimeSpan.FromDays(1)),
        ("h", TimeSpan.FromHours(1)),
        ("m", TimeSpan.FromMinutes(1)),
        (Seconds, TimeSpan.FromSeconds(1)),
        (Milliseconds, TimeSpan.FromMilliseconds(1)),
    ];

    /// <summary>The time written in nginx's way; <see langword="false"/> when the text is not one or is too long.</summary>
    public static bool TryParse(string text, out TimeSpan value)
    {
        value = TimeSpan.Zero;
        var parts = text.Split([' '], StringSplitOptions.RemoveEmptyEntries).SelectMany(PartsOf).ToList();
        if (parts.Count == 0)
            return false;

        long ticks = 0;
        foreach (var (number, unit) in parts)
        {
            if (number is null || LengthOf(unit) is not { } length || number > (long.MaxValue - ticks) / length.Ticks)
                return false;
            ticks += number.Value * length.Ticks;
        }
        value = TimeSpan.FromTicks(ticks);
        return true;
    }

    /// <summary>The time in the largest units first, e.g. 90 minutes is <c>1h30m</c>; less than a millisecond is dropped.</summary>
    public static string Format(TimeSpan time)
    {
        var text = new StringBuilder();
        var rest = time.Ticks;
        foreach (var (unit, length) in Written)
        {
            var count = rest / length.Ticks;
            if (count <= 0)
                continue;
            text.Append(count.ToString(CultureInfo.InvariantCulture)).Append(unit);
            rest -= count * length.Ticks;
        }
        return text.Length > 0 ? text.ToString() : "0" + Seconds;
    }

    /// <summary>
    /// The parts of one word, e.g. <c>1h30m</c> → (1, h), (30, m); a part without a number has no number.
    /// </summary>
    private static IEnumerable<(long? Number, string Unit)> PartsOf(string word)
    {
        var rest = word;
        while (rest.Length > 0)
        {
            var digits = new string(rest.TakeWhile(IsDigit).ToArray());
            rest = rest.Substring(digits.Length);
            var unit = rest.StartsWith(Milliseconds, StringComparison.Ordinal) ? Milliseconds
                : rest.Length > 0 && !IsDigit(rest[0]) ? rest.Substring(0, 1)
                : string.Empty;
            rest = rest.Substring(unit.Length);
            yield return (long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : null, unit);
        }
    }

    /// <summary>A digit as nginx reads it: 0 to 9 only.</summary>
    private static bool IsDigit(char c) => c is >= '0' and <= '9';

    /// <summary>The length of a unit, seconds for none; <see langword="null"/> for a text that is not a unit.</summary>
    private static TimeSpan? LengthOf(string unit) => unit switch
    {
        "" or Seconds => TimeSpan.FromSeconds(1),
        Milliseconds => TimeSpan.FromMilliseconds(1),
        "m" => TimeSpan.FromMinutes(1),
        "h" => TimeSpan.FromHours(1),
        "d" => TimeSpan.FromDays(1),
        "w" => TimeSpan.FromDays(7),
        "M" => TimeSpan.FromDays(30),
        "y" => TimeSpan.FromDays(365),
        _ => null,
    };
}
