using System.Collections;
using Lakasabasz.Nginx.Sdk.Internal.Structure;

namespace Lakasabasz.Nginx.Sdk.Views;

/// <summary>
/// Directives of a block read as entries of a dictionary, one directive per entry (<c>belongs</c> picks them,
/// <c>read</c> gives the entry of one, <c>write</c> the directive of an entry), e.g. <c>proxy_set_header Host
/// $host;</c> as the entry <c>Host</c> → <c>$host</c>. Read anew on each use.
/// </summary>
/// <remarks>
/// Setting a key rewrites the arguments of the first directive with it and removes the others; a new key is added
/// after the last directive of the block. A directive's name cannot change, so when the key is its name and is set in
/// another case (<c>TEXT/HTML</c> for <c>text/html</c>), the directive is replaced by a new one at the end. When the
/// file repeats a key, the first directive gives its value; removing a key removes all of them.
/// </remarks>
internal sealed class DirectiveDictionary<TValue>(
    StructureBlock block,
    Func<StructureDirective, bool> belongs,
    Func<StructureDirective, KeyValuePair<string, TValue>> read,
    Func<string, TValue, (string Name, IReadOnlyList<string> Arguments)> write,
    StringComparer comparer) : IDictionary<string, TValue>
{
    public int Count => Keys.Count;

    public bool IsReadOnly => false;

    public ICollection<string> Keys => Directives.Select(directive => read(directive).Key).Distinct(comparer).ToList();

    public ICollection<TValue> Values => Keys.Select(key => this[key]).ToList();

    private List<StructureDirective> Directives => block.Directives.Where(belongs).ToList();

    public TValue this[string key]
    {
        get => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException($"There is no \"{key}\".");
        set
        {
            var existing = DirectivesWith(key);
            var (name, arguments) = write(key, value);
            if (existing.Count == 0 || existing[0].Name != name)
            {
                RemoveAll(existing);
                block.Add(name, arguments).OrThrow();
                return;
            }

            existing[0].SetArguments(arguments);
            RemoveAll(existing.Skip(1));
        }
    }

    public void Add(string key, TValue value)
    {
        if (ContainsKey(key))
            throw new ArgumentException($"\"{key}\" is there already.", nameof(key));
        this[key] = value;
    }

    public void Add(KeyValuePair<string, TValue> item) => Add(item.Key, item.Value);

    public bool Remove(string key)
    {
        var existing = DirectivesWith(key);
        RemoveAll(existing);
        return existing.Count > 0;
    }

    public bool Remove(KeyValuePair<string, TValue> item) => Contains(item) && Remove(item.Key);

    public bool ContainsKey(string key) => DirectivesWith(key).Count > 0;

    public bool Contains(KeyValuePair<string, TValue> item) =>
        TryGetValue(item.Key, out var value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);

    public bool TryGetValue(string key, out TValue value)
    {
        if (DirectivesWith(key).FirstOrDefault() is { } directive)
        {
            value = read(directive).Value;
            return true;
        }

        value = default!;
        return false;
    }

    public void Clear() => RemoveAll(Directives);

    public void CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex) => Entries().CopyTo(array, arrayIndex);

    public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator() => Entries().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private List<KeyValuePair<string, TValue>> Entries() =>
        Keys.Select(key => new KeyValuePair<string, TValue>(key, this[key])).ToList();

    /// <summary>Every directive whose key is <paramref name="key"/>, in order.</summary>
    private List<StructureDirective> DirectivesWith(string key) => Directives.Where(directive => comparer.Equals(read(directive).Key, key)).ToList();

    private void RemoveAll(IEnumerable<StructureDirective> directives)
    {
        foreach (var directive in directives.ToList())
            block.Remove(directive).OrThrow();
    }
}
