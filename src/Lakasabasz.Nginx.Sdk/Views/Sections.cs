using Lakasabasz.Nginx.Sdk.Internal.Structure;

namespace Lakasabasz.Nginx.Sdk.Views;

/// <summary>A section that occurs once in its block, e.g. <c>http</c> in <c>nginx.conf</c>, read and set as a property.</summary>
internal static class Sections
{
    /// <summary>The first block of the name; <see langword="null"/> without any.</summary>
    public static T? Get<T>(StructureBlock block, string name, Func<StructureDirective, T> create)
        where T : Section => FirstOf(block, name) is { } directive ? Facades.Of(directive, create) : null;

    /// <summary>
    /// Makes <paramref name="section"/> the only block of the name: the others are removed, a loose section is added
    /// at the end. <see langword="null"/> removes them all.
    /// </summary>
    public static void Set(StructureBlock block, string name, Section? section)
    {
        if (section is not null && FirstOf(block, name) == section.Node)
            return;
        foreach (var directive in block.Directives.Where(directive => IsSection(directive, name)).ToList())
            block.Remove(directive).OrThrow();
        if (section is not null)
            block.Attach(section.Node).OrThrow();
    }

    private static StructureDirective? FirstOf(StructureBlock block, string name) =>
        block.Directives.FirstOrDefault(directive => IsSection(directive, name));

    private static bool IsSection(StructureDirective directive, string name) =>
        directive.Name == name && directive.Kind == DirectiveKind.Block;
}
