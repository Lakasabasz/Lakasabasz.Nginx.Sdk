using Lakasabasz.Nginx.Sdk.Internal.Structure;
using Lakasabasz.Nginx.Sdk.Views;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>A response header added by <c>add_header</c>, e.g. <c>add_header X-Frame-Options DENY always;</c>.</summary>
/// <param name="Name">The header's name.</param>
/// <param name="Value">The header's value; it may contain variables.</param>
public sealed record Header(string Name, string Value)
{
    private const string AlwaysFlag = "always";

    /// <summary>Added to every response, also to errors (<c>always</c>); otherwise only to 2xx and 3xx responses.</summary>
    public bool Always { get; init; }

    internal static Header Read(StructureDirective directive) => directive.Arguments switch
    {
        [var name, var value] => new Header(name, value),
        [var name, var value, AlwaysFlag] => new Header(name, value) { Always = true },
        _ => throw DirectiveValues.Unreadable(directive, "a header: a name, a value and optionally always"),
    };

    internal IReadOnlyList<string> ToArguments() => Always ? [Name, Value, AlwaysFlag] : [Name, Value];
}
