using System.Collections;
using Lakasabasz.Nginx.Sdk.Internal.Structure;

namespace Lakasabasz.Nginx.Sdk.Views;

/// <summary>
/// The arguments of every directive of one name in a block as one list, e.g. the names of a server when the file has
/// two <c>server_name</c> lines. Read anew on each use, so it always shows the directives as they are.
/// </summary>
/// <remarks>
/// A value is changed in the directive holding it; a new value goes to the last directive (a new one without any); a
/// directive left without arguments is removed.
/// </remarks>
internal sealed class ArgumentList(StructureBlock block, string name) : IList<string>
{
    public int Count => Directives.Sum(directive => directive.Arguments.Count);

    public bool IsReadOnly => false;

    private List<StructureDirective> Directives => block.Directives.Where(directive => directive.Name == name).ToList();

    public string this[int index]
    {
        get
        {
            var (directive, position) = Locate(index);
            return directive.Arguments[position];
        }
        set
        {
            var (directive, position) = Locate(index);
            RewriteArguments(directive, arguments => arguments[position] = value);
        }
    }

    public void Add(string item)
    {
        if (Directives.LastOrDefault() is { } last)
            RewriteArguments(last, arguments => arguments.Add(item));
        else
            block.Add(name, [item]).OrThrow();
    }

    public void Insert(int index, string item)
    {
        if (index == Count)
        {
            Add(item);
            return;
        }

        var (directive, position) = Locate(index);
        RewriteArguments(directive, arguments => arguments.Insert(position, item));
    }

    public void RemoveAt(int index)
    {
        var (directive, position) = Locate(index);
        if (directive.Arguments.Count == 1)
            block.Remove(directive).OrThrow();
        else
            RewriteArguments(directive, arguments => arguments.RemoveAt(position));
    }

    public bool Remove(string item)
    {
        var index = IndexOf(item);
        if (index < 0)
            return false;
        RemoveAt(index);
        return true;
    }

    public void Clear() => block.Remove(name).OrThrow();

    public int IndexOf(string item) => Values().IndexOf(item);

    public bool Contains(string item) => IndexOf(item) >= 0;

    public void CopyTo(string[] array, int arrayIndex) => Values().CopyTo(array, arrayIndex);

    public IEnumerator<string> GetEnumerator() => Values().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private List<string> Values() => Directives.SelectMany(directive => directive.Arguments).ToList();

    /// <summary>Gives the directive its arguments changed by <paramref name="change"/>.</summary>
    private static void RewriteArguments(StructureDirective directive, Action<List<string>> change)
    {
        var arguments = directive.Arguments.ToList();
        change(arguments);
        directive.SetArguments(arguments);
    }

    private (StructureDirective Directive, int Position) Locate(int index)
    {
        var rest = index;
        foreach (var directive in Directives)
        {
            if (rest >= 0 && rest < directive.Arguments.Count)
                return (directive, rest);
            rest -= directive.Arguments.Count;
        }
        throw new ArgumentOutOfRangeException(nameof(index), index, "There is no value at this index.");
    }
}
