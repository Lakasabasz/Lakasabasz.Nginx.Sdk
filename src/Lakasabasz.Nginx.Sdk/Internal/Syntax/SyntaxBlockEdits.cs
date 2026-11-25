namespace Lakasabasz.Nginx.Sdk.Internal.Syntax;

/// <summary>Editing the lines of a block: what is not edited stays as it was written, new text is formatted like its neighbours.</summary>
internal sealed partial class SyntaxBlock
{
    /// <summary>Whether a directive with a body (a block, also a raw one) is in this block: then nothing is added on one line.</summary>
    public bool HoldsBlock => Lines.Any(line => line.Items.Any(item => item.Node is SyntaxDirective { HasBody: true }));

    /// <summary>
    /// Removes directives: a line left without directives goes with its comments; a closing <c>}</c> that was on such
    /// a line gets a line of its own, indented like the block. Empty lines left at the start of the block go too: they
    /// separated a block from directives that are gone.
    /// </summary>
    /// <param name="removed">The directives to remove.</param>
    /// <param name="ownerIndentation">The indentation of the block directive's line; <see langword="null"/> for the top level of a file.</param>
    public void Remove(HashSet<SyntaxDirective> removed, string? ownerIndentation)
    {
        var oneLine = ownerIndentation is not null && IsOneLine;
        var removedLine = false;
        for (var i = Lines.Count - 1; i >= 0; i--)
        {
            var line = Lines[i];
            var startRemoved = line.Items.Count > 0 && IsRemoved(line.Items[0]);
            if (line.Items.RemoveAll(IsRemoved) == 0)
                continue;
            if (line.HasDirectives)
            {
                if (startRemoved)
                    line.Items[0].Spacing = string.Empty;
                continue;
            }

            var closesBlock = ownerIndentation is not null && i > 0 && i == Lines.Count - 1 && line.Ending.Length == 0;
            Lines.RemoveAt(i);
            removedLine = true;
            if (closesBlock)
                BeforeClosing = ownerIndentation!;
        }

        while (removedLine && Lines.Count > 0 && Lines[0].IsBlank)
            Lines.RemoveAt(0);

        if (oneLine && removedLine && Lines.Count == 0)
        {
            AfterOpening = string.Empty;
            BeforeClosing = string.Empty;
        }

        bool IsRemoved(SyntaxItem item) => item.Node is SyntaxDirective node && removed.Contains(node);
    }

    /// <summary>
    /// Adds a directive after the last one: on the same line in a block written on one line (a block or a comment puts
    /// such a block onto lines first), otherwise on a new line
    /// indented like the last directive (in a block without directives: after its last comment, one level deeper than
    /// the block; in a file without directives: at its end).
    /// </summary>
    /// <param name="create">Makes the directive, given the indentation of its line and whether it is on one line.</param>
    /// <param name="style">The style of the block's content: its line break and indentation unit.</param>
    /// <param name="ownerIndentation">The indentation of the block directive's line; <see langword="null"/> for the top level of a file.</param>
    /// <param name="holdsBlock">Whether a block is in this block already, so nothing goes on one line.</param>
    /// <param name="after">The node added just before in the same change: the new one goes after it, not after the last directive.</param>
    /// <returns>The node made by <paramref name="create"/>.</returns>
    public SyntaxNode Append(Func<string, bool, SyntaxNode> create, NginxStyle style, string? ownerIndentation, bool holdsBlock, SyntaxNode? after)
    {
        if (ownerIndentation is not null && IsOneLine && !holdsBlock)
            return AppendOnLine(create(string.Empty, true));

        if (ownerIndentation is not null && IsOneLine)
            Restyle(oneLine: false, style, ownerIndentation);
        return ownerIndentation is null && !Lines.Any(line => line.HasDirectives)
            ? AppendToFile(create(string.Empty, false), style)
            : AppendLine(create, style, ownerIndentation ?? string.Empty, after);
    }

    public string IndentationOf(SyntaxNode node) => LineOf(node).Indentation;

    /// <summary>
    /// Sets the comment after a directive on its line (<see langword="null"/> removes it). What followed the directive
    /// on the line moves to a line of its own, and a <c>}</c> on the line too, so the comment comments out nothing.
    /// </summary>
    public void SetTrailingComment(SyntaxDirective node, string? comment, NginxStyle style, string? ownerIndentation)
    {
        var line = LineOf(node);
        var next = line.Items.FindIndex(item => item.Node == node) + 1;
        var hasComment = next < line.Items.Count && line.Items[next].Node is SyntaxComment;
        if (comment is null)
        {
            if (hasComment)
                line.Items.RemoveAt(next);
            return;
        }

        var item = new SyntaxItem(hasComment ? line.Items[next].Spacing : " ", new SyntaxComment(comment));
        if (hasComment)
            line.Items[next] = item;
        else
            line.Items.Insert(next, item);

        if (next + 1 < line.Items.Count)
        {
            var rest = new SyntaxLine { Indentation = line.Indentation, Trailing = line.Trailing, Ending = line.Ending };
            rest.Items.AddRange(line.Items.Skip(next + 1));
            rest.Items[0].Spacing = string.Empty;
            line.Items.RemoveRange(next + 1, rest.Items.Count);
            line.Trailing = string.Empty;
            line.Ending = style.LineBreak;
            Lines.Insert(Lines.IndexOf(line) + 1, rest);
        }
        BreakLineBeforeClosingBrace(node, style, ownerIndentation);
    }

    /// <summary>Ends the line of <paramref name="node"/> with a line break when <c>}</c> follows it on the line.</summary>
    public void BreakLineBeforeClosingBrace(SyntaxNode node, NginxStyle style, string? ownerIndentation)
    {
        var line = LineOf(node);
        if (ownerIndentation is null || line.Ending.Length > 0)
            return;
        line.Trailing = string.Empty;
        line.Ending = style.LineBreak;
        BeforeClosing = ownerIndentation;
    }

    /// <summary>
    /// Writes the block on one line, or each directive on a line of its own. A block with comments or blocks in it
    /// stays on several lines.
    /// </summary>
    public void Restyle(bool oneLine, NginxStyle style, string ownerIndentation)
    {
        if (oneLine == IsOneLine)
            return;
        var items = Lines.SelectMany(line => line.Items).ToList();
        if (oneLine && (HoldsBlock || items.Any(item => item.Node is SyntaxComment)))
            return;

        Lines.Clear();
        if (oneLine)
        {
            AfterOpening = items.Count > 0 ? " " : string.Empty;
            BeforeClosing = " ";
            if (items.Count == 0)
                return;
            var line = new SyntaxLine();
            foreach (var item in items)
                line.Items.Add(new SyntaxItem(line.Items.Count == 0 ? string.Empty : " ", item.Node));
            Lines.Add(line);
            return;
        }

        AfterOpening = style.LineBreak;
        BeforeClosing = ownerIndentation;
        foreach (var item in items)
        {
            var line = new SyntaxLine { Indentation = ownerIndentation + style.Indentation, Ending = style.LineBreak };
            line.Items.Add(new SyntaxItem(string.Empty, item.Node));
            Lines.Add(line);
        }
    }

    /// <summary>
    /// The lines right above <paramref name="node"/>'s line become <paramref name="lines"/> (e.g. a block's comment):
    /// the lines <paramref name="old"/> are removed first.
    /// </summary>
    public void ReplaceLinesAbove(SyntaxNode node, IReadOnlyCollection<SyntaxLine> old, IReadOnlyList<SyntaxLine> lines)
    {
        Lines.RemoveAll(old.Contains);
        Lines.InsertRange(Lines.IndexOf(LineOf(node)), lines);
    }

    /// <summary>
    /// <paramref name="count"/> empty lines above <paramref name="top"/> (the first line of a block with its comment),
    /// unless no directive comes before it in this block.
    /// </summary>
    public void SpaceAbove(SyntaxLine top, int count, NginxStyle style)
    {
        var index = Lines.IndexOf(top);
        if (count == 0 || !Lines.Take(index).Any(line => line.HasDirectives))
            return;
        Lines.InsertRange(index, Enumerable.Range(0, count).Select(_ => new SyntaxLine { Ending = style.LineBreak }));
    }

    /// <summary>
    /// Removes the lines of a block's comment and the empty lines right above them (or above the block, without a
    /// comment), except those in <paramref name="kept"/>; the block itself is removed with <see cref="Remove"/>.
    /// </summary>
    public void RemoveLinesAbove(SyntaxNode node, IReadOnlyCollection<SyntaxLine> comment, IReadOnlyCollection<SyntaxLine> kept)
    {
        var line = LineOf(node);
        if (line.Items[0].Node != node)
            return;
        Lines.RemoveAll(comment.Contains);
        var index = Lines.IndexOf(line);
        while (index > 0 && Lines[index - 1].IsBlank && !kept.Contains(Lines[index - 1]))
        {
            Lines.RemoveAt(index - 1);
            index--;
        }
    }

    /// <summary>The lines at the top of a file become <paramref name="lines"/> (its comment); <paramref name="old"/> are removed first.</summary>
    public void ReplaceTopLines(IReadOnlyCollection<SyntaxLine> old, IReadOnlyList<SyntaxLine> lines)
    {
        Lines.RemoveAll(old.Contains);
        Lines.InsertRange(0, lines);
    }

    /// <summary>The line holding <paramref name="node"/>.</summary>
    public SyntaxLine LineOf(SyntaxNode node) => Lines.First(line => line.Items.Any(item => item.Node == node));

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

    private SyntaxNode AppendToFile(SyntaxNode directive, NginxStyle style)
    {
        if (Lines.Count > 0 && Lines[^1].Ending.Length == 0)
            Lines[^1].Ending = style.LineBreak;
        var line = new SyntaxLine { Ending = style.LineBreak };
        line.Items.Add(new SyntaxItem(string.Empty, directive));
        Lines.Add(line);
        return directive;
    }

    /// <summary>
    /// A new line after <paramref name="after"/>, the last directive, or the last comment. When that line ends at the
    /// block's <c>}</c> (no line break), the new line takes its place before the <c>}</c>: the old line gets the line
    /// break and the new one none.
    /// </summary>
    private SyntaxNode AppendLine(Func<string, bool, SyntaxNode> create, NginxStyle style, string ownerIndentation, SyntaxNode? after)
    {
        var lastDirective = after is not null ? Lines.IndexOf(LineOf(after)) : Lines.FindLastIndex(line => line.HasDirectives);
        var last = lastDirective >= 0 ? lastDirective : Lines.FindLastIndex(line => line.Items.Count > 0);
        var indentation = lastDirective >= 0 ? Lines[last].Indentation : ownerIndentation + style.Indentation;
        var line = new SyntaxLine { Indentation = indentation, Ending = style.LineBreak };
        var node = create(indentation, false);
        line.Items.Add(new SyntaxItem(string.Empty, node));
        if (last >= 0 && Lines[last].Ending.Length == 0)
        {
            Lines[last].Ending = style.LineBreak;
            line.Ending = string.Empty;
        }
        Lines.Insert(last + 1, line);
        return node;
    }
}
