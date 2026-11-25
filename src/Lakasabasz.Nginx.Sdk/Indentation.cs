namespace Lakasabasz.Nginx.Sdk;

/// <summary>One level of indentation: a number of spaces or of tabs, e.g. <c>Indentation.Spaces(4)</c> or <c>Indentation.Tabs()</c>.</summary>
public sealed record Indentation
{
    private Indentation(IndentCharacter character, int width)
    {
        Character = character;
        Width = width;
    }

    /// <summary>What a level is made of: spaces or tabs.</summary>
    public IndentCharacter Character { get; }

    /// <summary>How many of them make one level; at least 1.</summary>
    public int Width { get; }

    /// <summary>A level of <paramref name="width"/> spaces.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> is less than 1.</exception>
    public static Indentation Spaces(int width) => new(IndentCharacter.Space, AtLeastOne(width));

    /// <summary>A level of <paramref name="count"/> tabs, one by default.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than 1.</exception>
    public static Indentation Tabs(int count = 1) => new(IndentCharacter.Tab, AtLeastOne(count));

    /// <inheritdoc />
    public override string ToString() => $"{Width} {(Character == IndentCharacter.Tab ? "tab" : "space")}{(Width == 1 ? string.Empty : "s")}";

    private static int AtLeastOne(int width) => width >= 1
        ? width
        : throw new ArgumentOutOfRangeException(nameof(width), width, "A level of indentation is at least one character.");
}

/// <summary>What indentation is made of.</summary>
public enum IndentCharacter
{
    /// <summary>Spaces.</summary>
    Space,

    /// <summary>Tabs.</summary>
    Tab,
}
