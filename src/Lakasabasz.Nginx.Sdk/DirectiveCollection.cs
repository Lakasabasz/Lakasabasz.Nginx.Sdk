using System.Collections;
using System.Text;
using Lakasabasz.Nginx.Sdk.Internal;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>
/// The directives of a file or of a block, in order: the generic way to read and change any directive by its name and arguments.
/// </summary>
/// <remarks>
/// <para>Changes keep the rest of the file as it was; only the changed directive's own text is written anew:</para>
/// <list type="bullet">
/// <item><see cref="Add"/> and <see cref="AddBlock"/> append after the last directive, on a new line indented like
/// the previous directive (in an empty block: one level deeper than the block, with the file's indentation unit, a
/// tab or four spaces). In a block written on one line (<c>server { listen 80; }</c>) they stay on that line.</item>
/// <item><see cref="Remove(Directive)"/> removes the directive's line: its indentation, the directive, a comment
/// after it on the same line and the line break. The other directives of that line, if any, stay.</item>
/// <item><see cref="Set"/> leaves exactly one directive of the name: the first one gets the arguments, the others
/// are removed; without any, the directive is added.</item>
/// <item>Changed arguments are written separated by single spaces and quoted (<c>"…"</c>) only when nginx needs it:
/// empty, or with whitespace, quotes, <c>;</c>, <c>{</c>, <c>}</c>, a backslash, or a leading <c>#</c>.</item>
/// </list>
/// </remarks>
public sealed class DirectiveCollection : IReadOnlyList<Directive>
{
    /// <summary>Text between directives (whitespace, comments) as <see cref="string"/>, and <see cref="Directive"/>s.</summary>
    private readonly List<object> _items;
    private readonly bool _readOnly;

    internal DirectiveCollection(List<object> items, Formatting formatting, bool readOnly = false)
    {
        _items = items;
        Formatting = formatting;
        _readOnly = readOnly;
        foreach (var directive in _items.OfType<Directive>())
            directive.Parent = this;
    }

    /// <summary>The collection of a simple directive: empty and not changeable.</summary>
    internal static DirectiveCollection NotABlock => new([], Formatting.Default, readOnly: true);

    /// <summary>The block directive that holds this collection; <see langword="null"/> for the top level of a file.</summary>
    internal Directive? Owner { get; set; }

    internal Formatting Formatting { get; }

    /// <summary>Number of directives.</summary>
    public int Count => _items.Count(item => item is Directive);

    /// <summary>The directive at <paramref name="index"/>.</summary>
    public Directive this[int index] => _items.OfType<Directive>().ElementAt(index);

    /// <summary>Adds a simple directive, e.g. <c>Add("some_module_directive", "arg1", "arg2")</c>.</summary>
    /// <returns>The added directive.</returns>
    public Directive Add(string name, params string[] arguments) =>
        Insert(new Directive(name, arguments, DirectiveKind.Simple, null, 0, 0, null, null));

    /// <summary>Adds an empty block directive, e.g. <c>AddBlock("custom_block", "arg")</c>; fill it through its <see cref="Directive.Directives"/>.</summary>
    /// <returns>The added block.</returns>
    public Directive AddBlock(string name, params string[] arguments)
    {
        var (inline, indentation) = Placement();
        var body = inline ? " " : Formatting.Newline + indentation;
        return Insert(new Directive(name, arguments, DirectiveKind.Block, null, 0, 0,
            new DirectiveCollection([body], Formatting), null));
    }

    /// <summary>
    /// Makes <paramref name="name"/> occur once, with <paramref name="arguments"/>: the first such directive gets them,
    /// the others are removed; without any, the directive is added.
    /// </summary>
    /// <returns>The directive with the arguments.</returns>
    public Directive Set(string name, params string[] arguments)
    {
        var existing = this.Where(directive => directive.Name == name).ToList();
        if (existing.Count == 0)
            return Add(name, arguments);

        EnsureChangeable();
        existing[0].SetArguments(arguments);
        foreach (var duplicate in existing.Skip(1))
            Remove(duplicate);
        return existing[0];
    }

    /// <summary>Removes every directive called <paramref name="name"/> from this collection (not from nested blocks).</summary>
    /// <returns>How many were removed.</returns>
    public int Remove(string name) => this.Where(directive => directive.Name == name).ToList().Count(Remove);

    /// <summary>Removes <paramref name="directive"/> with its line (see the class remarks).</summary>
    /// <returns><see langword="false"/> when the directive is not in this collection.</returns>
    public bool Remove(Directive directive)
    {
        EnsureChangeable();
        var index = _items.IndexOf(directive);
        if (index < 0)
            return false;

        var before = index > 0 && _items[index - 1] is string previous ? previous : null;
        var after = index + 1 < _items.Count && _items[index + 1] is string next ? next : null;
        var (keptBefore, startsLine) = ConfigWriter.WithoutIndentation(before ?? string.Empty);
        var keptAfter = ConfigWriter.WithoutRestOfLine(after ?? string.Empty, startsLine);

        var first = before is null ? index : index - 1;
        var count = 1 + (before is null ? 0 : 1) + (after is null ? 0 : 1);
        _items.RemoveRange(first, count);
        _items.Insert(first, keptBefore + keptAfter);
        directive.Parent = null;
        return true;
    }

    /// <inheritdoc />
    public IEnumerator<Directive> GetEnumerator() => _items.OfType<Directive>().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal void Render(StringBuilder output)
    {
        foreach (var item in _items)
        {
            if (item is Directive directive)
                directive.Render(output);
            else
                output.Append((string)item);
        }
    }

    private Directive Insert(Directive directive)
    {
        EnsureChangeable();
        var (inline, indentation) = Placement();
        var last = _items.FindLastIndex(item => item is Directive);
        if (Owner is null && last < 0)
        {
            // A file without directives: append, on a line of its own, and end the file with a line break
            var separator = _items.Count > 0 && _items[_items.Count - 1] is string { Length: > 0 } && !EndsWithNewline(_items[_items.Count - 1])
                ? Formatting.Newline
                : string.Empty;
            _items.Add(separator);
            _items.Add(directive);
            _items.Add(Formatting.Newline);
        }
        else
        {
            var position = last >= 0 ? last + 1 : 0;
            _items.Insert(position, inline ? " " : Formatting.Newline + indentation);
            _items.Insert(position + 1, directive);
            if (inline && position + 2 == _items.Count)
                _items.Add(" ");
        }
        directive.Parent = this;
        return directive;
    }

    /// <summary>Where a new directive goes: on the same line (one-line block) or on a new line with this indentation.</summary>
    private (bool Inline, string Indentation) Placement()
    {
        var multiline = Owner is null || _items.OfType<string>().Any(text => text.IndexOf('\n') >= 0)
            || this.Any(directive => directive.IsBlock);
        if (!multiline)
            return (true, string.Empty);

        var last = _items.FindLastIndex(item => item is Directive);
        if (last >= 0)
            return (false, IndentationBefore(last));
        return (false, Owner is null ? string.Empty : Owner.Parent!.IndentationOf(Owner) + Formatting.IndentUnit);
    }

    private string IndentationOf(Directive directive) => IndentationBefore(_items.IndexOf(directive));

    private string IndentationBefore(int index)
    {
        if (index <= 0 || _items[index - 1] is not string before)
            return Owner is null || index > 0 ? string.Empty : Owner.Parent!.IndentationOf(Owner) + Formatting.IndentUnit;
        var lineStart = before.LastIndexOf('\n');
        var tail = before.Substring(lineStart + 1);
        return tail.All(c => c is ' ' or '\t') ? tail : string.Empty;
    }

    private static bool EndsWithNewline(object item) => item is string text && text.EndsWith("\n", StringComparison.Ordinal);

    private void EnsureChangeable()
    {
        if (_readOnly)
            throw new InvalidOperationException("Only a block directive contains directives.");
    }
}
