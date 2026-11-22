using System.Collections;
using Lakasabasz.Nginx.Sdk.Internal.Structure;

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
    private readonly StructureBlock _block;

    internal DirectiveCollection(StructureBlock block)
    {
        _block = block;
    }

    /// <summary>Number of directives.</summary>
    public int Count => _block.Directives.Count;

    /// <summary>The directive at <paramref name="index"/>.</summary>
    public Directive this[int index] => Facades.Of(_block.Directives[index]);

    /// <summary>Adds a simple directive, e.g. <c>Add("some_module_directive", "arg1", "arg2")</c>.</summary>
    /// <returns>The added directive.</returns>
    public Directive Add(string name, params string[] arguments) => Facades.Of(_block.Add(name, arguments).OrThrow());

    /// <summary>Adds an empty block directive, e.g. <c>AddBlock("custom_block", "arg")</c>; fill it through its <see cref="Directive.Directives"/>.</summary>
    /// <returns>The added block.</returns>
    public Directive AddBlock(string name, params string[] arguments) => Facades.Of(_block.AddBlock(name, arguments).OrThrow());

    /// <summary>
    /// Makes <paramref name="name"/> occur once, with <paramref name="arguments"/>: the first such directive gets them,
    /// the others are removed; without any, the directive is added.
    /// </summary>
    /// <returns>The directive with the arguments.</returns>
    public Directive Set(string name, params string[] arguments) => Facades.Of(_block.Set(name, arguments).OrThrow());

    /// <summary>Removes every directive called <paramref name="name"/> from this collection (not from nested blocks).</summary>
    /// <returns>How many were removed.</returns>
    public int Remove(string name) => _block.Remove(name).OrThrow();

    /// <summary>Removes <paramref name="directive"/> with its line (see the class remarks).</summary>
    /// <returns><see langword="false"/> when the directive is not in this collection.</returns>
    public bool Remove(Directive directive) => _block.Remove(directive.Node).OrThrow();

    /// <inheritdoc />
    public IEnumerator<Directive> GetEnumerator() => _block.Directives.Select(Facades.Of).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
