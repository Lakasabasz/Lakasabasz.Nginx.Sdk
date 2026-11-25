using System.Collections;
using Lakasabasz.Nginx.Sdk.Internal.Structure;
using Lakasabasz.Nginx.Sdk.Internal.Syntax;
using Lakasabasz.Nginx.Sdk.Views;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>
/// The sections of one kind in a file or block, e.g. the servers of a site, in order. Chosen with LINQ; added with
/// <c>Add</c> (a new section, or a loose one made in code) or <c>AddFromText</c> (nginx text), removed with
/// <see cref="Remove"/> or <see cref="Section.Remove"/>.
/// </summary>
/// <typeparam name="T">The kind of section.</typeparam>
public abstract class SectionCollection<T> : IReadOnlyCollection<T>
    where T : Section
{
    private readonly SectionScope _scope;
    private readonly string _name;
    private readonly Func<StructureDirective, T> _create;

    /// <param name="scope">Where the sections are found and added.</param>
    /// <param name="name">The name of the sections' block directive, e.g. <c>server</c>.</param>
    /// <param name="create">Makes the section of a directive read from a file.</param>
    private protected SectionCollection(SectionScope scope, string name, Func<StructureDirective, T> create)
    {
        _scope = scope;
        _name = name;
        _create = create;
    }

    /// <summary>Number of sections.</summary>
    public int Count => Items.Count();

    private IEnumerable<T> Items => _scope.Directives
        .Where(directive => directive.Name == _name && directive.Kind == DirectiveKind.Block)
        .Select(directive => Facades.Of(directive, _create));

    /// <summary>Adds a section made in code at the end; from now on it is part of this file or block.</summary>
    /// <returns>The same <paramref name="section"/>.</returns>
    /// <exception cref="InvalidOperationException">The section is in a file or block already.</exception>
    public T Add(T section)
    {
        _scope.Target.Attach(section.Node).OrThrow();
        return section;
    }

    /// <summary>Removes <paramref name="section"/>; it becomes loose and can be added again.</summary>
    /// <returns><see langword="false"/> when the section is not in this collection.</returns>
    public bool Remove(T section) =>
        Items.Contains(section) && section.Node.Parent is { } parent && parent.Remove(section.Node).OrThrow();

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>A new, empty section with these arguments (its key, e.g. a path) at the end.</summary>
    private protected T AddEmpty(params string[] arguments) => Facades.Of(_scope.Target.AddBlock(_name, arguments).OrThrow(), _create);

    /// <summary>A new section with these arguments at the end, its body parsed from nginx <paramref name="text"/>.</summary>
    /// <exception cref="NginxParseException">The text has a syntax error.</exception>
    private protected T AddParsed(IReadOnlyList<string> arguments, string text)
    {
        var directive = StructureDirective.NewBlock(_name, arguments, SyntaxTree.Fragment(text).OrThrow());
        return Facades.Of(_scope.Target.Attach(directive).OrThrow(), _create);
    }
}

/// <summary>The <c>server</c> blocks of a site, of <c>http</c>, or of a whole file.</summary>
public sealed class ServerCollection : SectionCollection<Server>
{
    internal ServerCollection(SectionScope scope)
        : base(scope, Server.Keyword, directive => new Server(directive))
    {
    }

    /// <summary>Adds an empty <c>server { }</c> at the end.</summary>
    public Server Add() => AddEmpty();

    /// <summary>Adds a server whose content is nginx text, e.g. <c>"server_name example.com;\nlisten 80;"</c>.</summary>
    /// <param name="text">The directives inside the block, without <c>server {</c> and <c>}</c>.</param>
    /// <exception cref="NginxParseException">The text has a syntax error; its line and column are in the text.</exception>
    public Server AddFromText(string text) => AddParsed([], text);
}

/// <summary>The <c>location</c> blocks of a server or of a location.</summary>
public sealed class LocationCollection : SectionCollection<Location>
{
    internal LocationCollection(SectionScope scope)
        : base(scope, Location.Keyword, directive => new Location(directive))
    {
    }

    /// <summary>Adds an empty prefix location, e.g. <c>location /api { }</c>, at the end.</summary>
    public Location Add(string path) => AddEmpty(path);

    /// <summary>Adds an empty location matched by <paramref name="modifier"/>, e.g. <c>location = /health { }</c>.</summary>
    public Location Add(LocationModifier modifier, string path) => AddEmpty(Location.ArgumentsOf(modifier, path));

    /// <summary>Adds a prefix location whose content is nginx text, e.g. <c>"proxy_pass http://127.0.0.1:5000;"</c>.</summary>
    /// <exception cref="NginxParseException">The text has a syntax error; its line and column are in the text.</exception>
    public Location AddFromText(string path, string text) => AddParsed([path], text);

    /// <summary>Adds a location matched by <paramref name="modifier"/>, its content nginx text.</summary>
    /// <exception cref="NginxParseException">The text has a syntax error; its line and column are in the text.</exception>
    public Location AddFromText(LocationModifier modifier, string path, string text) => AddParsed(Location.ArgumentsOf(modifier, path), text);
}

/// <summary>The <c>upstream</c> blocks of <c>http</c>, of a file included in it, or of a whole file.</summary>
public sealed class UpstreamCollection : SectionCollection<Upstream>
{
    internal UpstreamCollection(SectionScope scope)
        : base(scope, Upstream.Keyword, directive => new Upstream(directive))
    {
    }

    /// <summary>Adds an empty <c>upstream name { }</c> at the end.</summary>
    public Upstream Add(string name) => AddEmpty(name);

    /// <summary>Adds an upstream whose content is nginx text, e.g. <c>"server 127.0.0.1:5000;\nkeepalive 16;"</c>.</summary>
    /// <exception cref="NginxParseException">The text has a syntax error; its line and column are in the text.</exception>
    public Upstream AddFromText(string name, string text) => AddParsed([name], text);
}
