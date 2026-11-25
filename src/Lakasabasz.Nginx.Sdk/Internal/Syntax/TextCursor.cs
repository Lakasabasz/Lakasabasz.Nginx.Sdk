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

    /// <summary>The index of the current character in the text; past the end after skipping an escape at the end of the text.</summary>
    public int Index { get; private set; }

    /// <summary>Where the cursor is: the index of the current character and its 1-based line and column; reading it does not move the cursor.</summary>
    public TextPosition CurrentPosition
    {
        get
        {
            var line = Array.BinarySearch(_lineStarts, Index);
            line = line >= 0 ? line : ~line - 1;
            return new TextPosition(Index, line + 1, Index - _lineStarts[line] + 1);
        }
    }

    public bool AtEnd => Index >= Text.Length;

    /// <summary>The current character; <c>\0</c> at the end.</summary>
    public char Current => Peek(0);

    /// <summary>Whether a line break (<c>\n</c> or <c>\r\n</c>) starts here.</summary>
    public bool AtLineBreak => LineBreakLength > 0;

    public int LineBreakLength => Current == '\n' ? 1 : Current == '\r' && Peek(1) == '\n' ? 2 : 0;

    /// <summary>The character <paramref name="offset"/> places ahead; <c>\0</c> past the end.</summary>
    public char Peek(int offset) => Index + offset < Text.Length ? Text[Index + offset] : '\0';

    public void Advance(int count = 1) => Index += count;

    public void MoveTo(int index) => Index = index;

    /// <summary>The text from <paramref name="start"/> to the current position; reading it does not move the cursor.</summary>
    public string ReadSince(TextPosition start) => Text.Substring(start.Index, Math.Min(Index, Text.Length) - start.Index);
}

/// <summary>A place in a text: the index of its character, and its 1-based line and column for people.</summary>
internal readonly record struct TextPosition(int Index, int Line, int Column);
