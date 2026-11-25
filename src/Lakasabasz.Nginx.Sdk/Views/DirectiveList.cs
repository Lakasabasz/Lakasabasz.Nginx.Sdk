using System.Collections;
using Lakasabasz.Nginx.Sdk.Internal.Structure;

namespace Lakasabasz.Nginx.Sdk.Views;

/// <summary>
/// The directives of one name in a block as a list of typed values, one directive per value, e.g. the
/// <c>listen</c> directives of a server as <see cref="Listen"/> values. Read anew on each use.
/// </summary>
/// <remarks>
/// The values are immutable records, so changing one in place changes nothing: set a new value at its index instead,
/// which rewrites that directive. New
/// values are added at the end; inserting before another value is not possible, as directives are only added after
/// the last directive of the block.
/// </remarks>
internal sealed class DirectiveList<T>(
    StructureBlock block,
    string name,
    Func<StructureDirective, T> read,
    Func<T, IReadOnlyList<string>> write) : IList<T>
{
    public int Count => Directives.Count;

    public bool IsReadOnly => false;

    private List<StructureDirective> Directives => block.Directives.Where(directive => directive.Name == name).ToList();

    public T this[int index]
    {
        get => read(Directives[index]);
        set => Directives[index].SetArguments(write(value));
    }

    public void Add(T item) => block.Add(name, write(item)).OrThrow();

    public void Insert(int index, T item)
    {
        if (index != Count)
            throw new NotSupportedException("A directive is added after the last one of its block: use Add.");
        Add(item);
    }

    public void RemoveAt(int index) => block.Remove(Directives[index]).OrThrow();

    public bool Remove(T item)
    {
        var index = IndexOf(item);
        if (index < 0)
            return false;
        RemoveAt(index);
        return true;
    }

    public void Clear() => block.Remove(name).OrThrow();

    public int IndexOf(T item) => Values().IndexOf(item);

    public bool Contains(T item) => IndexOf(item) >= 0;

    public void CopyTo(T[] array, int arrayIndex) => Values().CopyTo(array, arrayIndex);

    public IEnumerator<T> GetEnumerator() => Values().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private List<T> Values() => Directives.Select(read).ToList();
}
