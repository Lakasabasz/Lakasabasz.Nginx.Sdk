namespace Lakasabasz.Nginx.Sdk.Internal.Syntax;

/// <summary>Editing the lines of a block: what is not edited stays as it was written, new text is formatted like its neighbours.</summary>
internal sealed partial class SyntaxBlock
{
    /// <summary>Whether a directive with a body (a block, also a raw one) is in this block: then nothing is added on one line.</summary>
    public bool HoldsBlock => Lines.Any(line => line.Items.Any(item => item.Node is SyntaxDirective { HasBody: true }));

    /// <summary>
    /// Removes directives: from their lines, then the lines left without directives (with their comments), then the
    /// empty lines left at the start of the block (they separated a block from directives that are gone); a block on
    /// one line that is left empty becomes <c>{}</c>.
    /// </summary>
    /// <param name="removed">The directives to remove.</param>
    /// <param name="ownerIndentation">The indentation of the block directive's line; <see langword="null"/> for the top level of a file.</param>
    public void Remove(HashSet<SyntaxDirective> removed, string? ownerIndentation)
    {
        var wasOneLine = ownerIndentation is not null && IsOneLine;
        var emptied = new List<SyntaxLine>();
        foreach (var line in Lines)
        {
            if (RemoveItems(line, removed))
                emptied.Add(line);
        }
        if (emptied.Count == 0)
            return;

        foreach (var line in Enumerable.Reverse(emptied))
            RemoveLine(line, ownerIndentation);
        while (Lines.Count > 0 && Lines[0].IsBlank)
            Lines.RemoveAt(0);
        if (wasOneLine && Lines.Count == 0)
        {
            AfterOpening = string.Empty;
            BeforeClosing = string.Empty;
        }
    }

    /// <summary>
    /// Removes the directives of <paramref name="removed"/> from the line, with the comments after them; the first
    /// directive left starts the line without spacing.
    /// </summary>
    /// <returns>Whether the line lost directives and has none left, so it goes too.</returns>
    private static bool RemoveItems(SyntaxLine line, HashSet<SyntaxDirective> removed)
    {
        var firstRemoved = line.Items.Count > 0 && IsIn(line.Items[0], removed);
        if (line.Items.RemoveAll(item => IsIn(item, removed)) == 0)
            return false;
        if (!line.HasDirectives)
            return true;
        if (firstRemoved)
            line.Items[0].Spacing = string.Empty;
        return false;
    }

    /// <summary>Removes a line; when it ended at the block's <c>}</c>, the <c>}</c> gets a line of its own, indented like the block.</summary>
    private void RemoveLine(SyntaxLine line, string? ownerIndentation)
    {
        var index = Lines.IndexOf(line);
        var endedAtBrace = ownerIndentation is not null && index > 0 && index == Lines.Count - 1 && !line.EndsWithLineBreak;
        Lines.RemoveAt(index);
        if (endedAtBrace)
            BeforeClosing = ownerIndentation!;
    }

    private static bool IsIn(SyntaxItem item, HashSet<SyntaxDirective> removed) =>
        item.Node is SyntaxDirective node && removed.Contains(node);

    /// <summary>
    /// Adds a directive after the last one: on the same line in a block written on one line, otherwise on a new line
    /// indented like the last directive (in a block without directives: after its last comment, one level deeper than
    /// the block; in a file without directives: at its end).
    /// </summary>
    /// <param name="create">Makes the directive, given the indentation of its line and whether it is on one line.</param>
    /// <param name="style">The style of the block's content: its line break and indentation unit.</param>
    /// <param name="ownerIndentation">The indentation of the block directive's line; <see langword="null"/> for the top level of a file.</param>
    /// <param name="holdsBlock">Whether a block is in this block already, so nothing goes on one line.</param>
    /// <param name="after">The node added just before in the same change: the new one goes after it, not after the last directive.</param>
    /// <returns>The node made by <paramref name="create"/>.</returns>
    public SyntaxNode Append(Func<string, bool, SyntaxNode> create, TextStyle style, string? ownerIndentation, bool holdsBlock, SyntaxNode? after)
    {
        if (ownerIndentation is not null && IsOneLine && !holdsBlock)
            return AppendOnLine(create(string.Empty, true));
        return ownerIndentation is null && !Lines.Any(line => line.HasDirectives)
            ? AppendToFile(create(string.Empty, false), style)
            : AppendLine(create, style, ownerIndentation ?? string.Empty, after);
    }

    public string IndentationOf(SyntaxNode node) => LineOf(node).Indentation;

    /// <summary>The line holding <paramref name="node"/>.</summary>
    private SyntaxLine LineOf(SyntaxNode node) => Lines.First(line => line.Items.Any(item => item.Node == node));

    private SyntaxNode AppendOnLine(SyntaxNode directive)
    {
        if (Lines.Count == 0)
        {
            Lines.Add(new SyntaxLine());
            if (AfterOpening.Length == 0 && BeforeClosing.Length == 0)
                BeforeClosing = " ";
        }
        Lines[^1].Items.Add(new SyntaxItem(" ", directive));
        return directive;
    }

    private SyntaxNode AppendToFile(SyntaxNode directive, TextStyle style)
    {
        if (Lines.Count > 0 && Lines[^1].Ending.Length == 0)
            Lines[^1].Ending = style.LineBreak;
        Lines.Add(SyntaxLine.Holding(directive, string.Empty, style.LineBreak));
        return directive;
    }

    /// <summary>
    /// A new line after <paramref name="after"/>, the last directive, or the last comment. When that line ends at the
    /// block's <c>}</c> (no line break), the new line takes its place before the <c>}</c>: the old line gets the line
    /// break and the new one none.
    /// </summary>
    private SyntaxNode AppendLine(Func<string, bool, SyntaxNode> create, TextStyle style, string ownerIndentation, SyntaxNode? after)
    {
        var lastDirective = after is not null ? Lines.IndexOf(LineOf(after)) : Lines.FindLastIndex(line => line.HasDirectives);
        var last = lastDirective >= 0 ? lastDirective : Lines.FindLastIndex(line => line.Items.Count > 0);
        var indentation = lastDirective >= 0 ? Lines[last].Indentation : ownerIndentation + style.Indentation;
        var node = create(indentation, false);
        var line = SyntaxLine.Holding(node, indentation, style.LineBreak);
        if (last >= 0 && Lines[last].Ending.Length == 0)
        {
            Lines[last].Ending = style.LineBreak;
            line.Ending = string.Empty;
        }
        Lines.Insert(last + 1, line);
        return node;
    }
}
