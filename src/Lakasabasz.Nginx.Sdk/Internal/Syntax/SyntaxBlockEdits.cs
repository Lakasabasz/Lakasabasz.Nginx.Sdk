namespace Lakasabasz.Nginx.Sdk.Internal.Syntax;

/// <summary>Editing the lines of a block: what is not edited stays as it was written, new text is formatted like its neighbours.</summary>
internal sealed partial class SyntaxBlock
{
    /// <summary>Whether a directive with a body (a block, also a raw one) is in this block: then nothing is added on one line.</summary>
    public bool HoldsBlock => Lines.Any(line => line.Items.Any(item => item.Node is SyntaxDirective { HasBody: true }));

    /// <summary>
    /// Removes directives: a line left without directives goes with its comments; a closing <c>}</c> that was on such
    /// a line gets a line of its own, indented like the block.
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

        if (oneLine && removedLine && Lines.Count == 0)
        {
            AfterOpening = string.Empty;
            BeforeClosing = string.Empty;
        }

        bool IsRemoved(SyntaxItem item) => item.Node is SyntaxDirective node && removed.Contains(node);
    }

    /// <summary>
    /// Adds a directive after the last one: on the same line in a block written on one line, otherwise on a new line
    /// indented like the last directive (in a block without directives: after its last comment, one level deeper than
    /// the block; in a file without directives: at its end).
    /// </summary>
    /// <param name="create">Makes the directive, given the indentation of its line and whether it is on one line.</param>
    /// <param name="formatting">The line break and indentation unit of the file.</param>
    /// <param name="ownerIndentation">The indentation of the block directive's line; <see langword="null"/> for the top level of a file.</param>
    /// <param name="holdsBlock">Whether a block is in this block already, so nothing goes on one line.</param>
    public void Append(Func<string, bool, SyntaxNode> create, Formatting formatting, string? ownerIndentation, bool holdsBlock)
    {
        if (ownerIndentation is not null && IsOneLine && !holdsBlock)
            AppendOnLine(create(string.Empty, true));
        else if (ownerIndentation is null && !Lines.Any(line => line.HasDirectives))
            AppendToFile(create(string.Empty, false), formatting);
        else
            AppendLine(create, formatting, ownerIndentation ?? string.Empty);
    }

    public string IndentationOf(SyntaxNode node) => LineOf(node).Indentation;

    /// <summary>
    /// Sets the comment after a directive on its line (<see langword="null"/> removes it). What followed the directive
    /// on the line moves to a line of its own, and a <c>}</c> on the line too, so the comment comments out nothing.
    /// </summary>
    public void SetTrailingComment(SyntaxDirective node, string? comment, Formatting formatting, string? ownerIndentation)
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
            line.Ending = formatting.Newline;
            Lines.Insert(Lines.IndexOf(line) + 1, rest);
        }
        EndLine(node, formatting, ownerIndentation);
    }

    /// <summary>Ends the line of <paramref name="node"/> with a line break when <c>}</c> follows it on the line.</summary>
    public void EndLine(SyntaxNode node, Formatting formatting, string? ownerIndentation)
    {
        var line = LineOf(node);
        if (ownerIndentation is null || line.Ending.Length > 0)
            return;
        line.Trailing = string.Empty;
        line.Ending = formatting.Newline;
        BeforeClosing = ownerIndentation;
    }

    /// <summary>
    /// Writes the block on one line, or each directive on a line of its own. A block with comments or blocks in it
    /// stays on several lines.
    /// </summary>
    public void Restyle(bool oneLine, Formatting formatting, string ownerIndentation)
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

        AfterOpening = formatting.Newline;
        BeforeClosing = ownerIndentation;
        foreach (var item in items)
        {
            var line = new SyntaxLine { Indentation = ownerIndentation + formatting.IndentUnit, Ending = formatting.Newline };
            line.Items.Add(new SyntaxItem(string.Empty, item.Node));
            Lines.Add(line);
        }
    }

    private SyntaxLine LineOf(SyntaxNode node) => Lines.First(line => line.Items.Any(item => item.Node == node));

    private void AppendOnLine(SyntaxNode directive)
    {
        if (Lines.Count == 0)
        {
            Lines.Add(new SyntaxLine());
            if (AfterOpening.Length == 0 && BeforeClosing.Length == 0)
                BeforeClosing = " ";
        }
        Lines[^1].Items.Add(new SyntaxItem(" ", directive));
    }

    private void AppendToFile(SyntaxNode directive, Formatting formatting)
    {
        if (Lines.Count > 0 && Lines[^1].Ending.Length == 0)
            Lines[^1].Ending = formatting.Newline;
        var line = new SyntaxLine { Ending = formatting.Newline };
        line.Items.Add(new SyntaxItem(string.Empty, directive));
        Lines.Add(line);
    }

    private void AppendLine(Func<string, bool, SyntaxNode> create, Formatting formatting, string ownerIndentation)
    {
        var lastDirective = Lines.FindLastIndex(line => line.HasDirectives);
        var last = lastDirective >= 0 ? lastDirective : Lines.FindLastIndex(line => line.Items.Count > 0);
        var indentation = lastDirective >= 0 ? Lines[last].Indentation : ownerIndentation + formatting.IndentUnit;
        var line = new SyntaxLine { Indentation = indentation, Ending = formatting.Newline };
        line.Items.Add(new SyntaxItem(string.Empty, create(indentation, false)));
        if (last >= 0 && Lines[last].Ending.Length == 0)
        {
            Lines[last].Ending = formatting.Newline;
            line.Ending = string.Empty;
        }
        Lines.Insert(last + 1, line);
    }
}
