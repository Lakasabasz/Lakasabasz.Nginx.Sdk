namespace Lakasabasz.Nginx.Sdk.Internal.Syntax;

/// <summary>A position in a text being read, moving forward only.</summary>
internal sealed class TextCursor
{
    private readonly int[] _lineStarts;

    public TextCursor(string text)
    {
        Text = text;
        _lineStarts = [0, .. text.Select((c, index) => (c, index)).Where(pair => pair.c == '\n').Select(pair => pair.index + 1)];
    }

    public string Text { get; }

    /// <summary>The index of the current character; past the end after skipping an escape at the end of the text.</summary>
    public int Position { get; private set; }

    public bool AtEnd => Position >= Text.Length;

    /// <summary>The current character; <c>\0</c> at the end.</summary>
    public char Current => Peek(0);

    /// <summary>Whether a line break (<c>\n</c> or <c>\r\n</c>) starts here.</summary>
    public bool AtLineBreak => LineBreakLength > 0;

    public int LineBreakLength => Current == '\n' ? 1 : Current == '\r' && Peek(1) == '\n' ? 2 : 0;

    /// <summary>The character <paramref name="offset"/> places ahead; <c>\0</c> past the end.</summary>
    public char Peek(int offset) => Position + offset < Text.Length ? Text[Position + offset] : '\0';

    public void Advance(int count = 1) => Position += count;

    public void MoveTo(int position) => Position = position;

    /// <summary>The text from <paramref name="start"/> to the current position.</summary>
    public string Take(int start) => Text.Substring(start, Math.Min(Position, Text.Length) - start);

    /// <summary>The 1-based line and column of a position.</summary>
    public (int Line, int Column) LineAndColumn(int position)
    {
        var index = Array.BinarySearch(_lineStarts, position);
        var line = index >= 0 ? index : ~index - 1;
        return (line + 1, position - _lineStarts[line] + 1);
    }
}
