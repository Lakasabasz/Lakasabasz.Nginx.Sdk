using Lakasabasz.Nginx.Sdk.Internal.Structure;
using Lakasabasz.Nginx.Sdk.Views;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>A <c>location { }</c> block: how requests for matching paths are handled.</summary>
public sealed class Location : HttpSection
{
    internal const string Keyword = "location";

    private const string ProxyPassName = "proxy_pass";
    private const string AliasName = "alias";
    private const string TryFilesName = "try_files";

    /// <summary>A loose prefix location, e.g. <c>new Location("/api")</c>, in no file until it is added to a <see cref="LocationCollection"/>.</summary>
    public Location(string path)
        : this(LocationModifier.None, path)
    {
    }

    /// <summary>A loose location matched by <paramref name="modifier"/>, e.g. <c>new Location(LocationModifier.Exact, "/health")</c>.</summary>
    public Location(LocationModifier modifier, string path)
        : base(Keyword, ArgumentsOf(modifier, path))
    {
    }

    internal Location(StructureDirective node)
        : base(node)
    {
    }

    /// <summary>
    /// The path, prefix or regex the location matches, e.g. <c>/api</c> or <c>\.php$</c>; <c>@name</c> for a named
    /// location. Setting it (or <see cref="Modifier"/>) writes the modifier as a word of its own: <c>=/x</c> becomes <c>= /x</c>.
    /// </summary>
    public string Path
    {
        get => Key.Path;
        set => Node.SetArguments(ArgumentsOf(Modifier, value));
    }

    /// <summary>How the path matches: a prefix (<see cref="LocationModifier.None"/>), exactly, or as a regex.</summary>
    public LocationModifier Modifier
    {
        get => Key.Modifier;
        set => Node.SetArguments(ArgumentsOf(value, Path));
    }

    /// <summary>The locations nested in this one.</summary>
    public LocationCollection Locations => new(SectionScope.Of(Block));

    /// <summary>The proxied server requests are passed to, e.g. <c>http://127.0.0.1:5000</c> (<c>proxy_pass</c>).</summary>
    public string? ProxyPass
    {
        get => DirectiveValues.GetText(Block, ProxyPassName);
        set => DirectiveValues.SetText(Block, ProxyPassName, value);
    }

    /// <summary>The directory or file that replaces the matched path (<c>alias</c>).</summary>
    public string? Alias
    {
        get => DirectiveValues.GetText(Block, AliasName);
        set => DirectiveValues.SetText(Block, AliasName, value);
    }

    /// <summary>The files tried in order, the last one being the fallback, e.g. <c>$uri $uri/ =404</c> (<c>try_files</c>).</summary>
    public IList<string> TryFiles => new ArgumentList(Block, TryFilesName);

    /// <summary>The <c>limit_except</c> block: what applies to the other HTTP methods; <see langword="null"/> removes it.</summary>
    public LimitExcept? LimitExcept
    {
        get => Sections.Get(Block, Sdk.LimitExcept.Keyword, directive => new LimitExcept(directive));
        set => Sections.Set(Block, Sdk.LimitExcept.Keyword, value);
    }

    private LocationKey Key => LocationKey.Read(Node.Arguments) ?? throw DirectiveValues.NotA(Node, "a path with an optional modifier");

    internal static string[] ArgumentsOf(LocationModifier modifier, string path) => new LocationKey(modifier, path).Arguments();
}
