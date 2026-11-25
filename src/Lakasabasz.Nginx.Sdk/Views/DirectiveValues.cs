using Lakasabasz.Nginx.Sdk.Internal.Structure;

namespace Lakasabasz.Nginx.Sdk.Views;

/// <summary>Values of a block's directives read by the sections.</summary>
internal static class DirectiveValues
{
    /// <summary>The error for a directive whose arguments a section cannot read, e.g. a location without a path.</summary>
    public static InvalidOperationException Unreadable(StructureDirective directive, string what)
    {
        var place = directive.Line > 0 ? $" at line {directive.Line}" : string.Empty;
        return new InvalidOperationException(
            $"{directive.Name}{place} is \"{string.Join(" ", directive.Arguments)}\", not {what}: read it through Directives.");
    }
}
