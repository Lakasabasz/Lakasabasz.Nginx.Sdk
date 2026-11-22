using System.Runtime.CompilerServices;
using Lakasabasz.Nginx.Sdk.Internal;
using Lakasabasz.Nginx.Sdk.Internal.Structure;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>
/// The public view of each internal object, made once and then always the same object, so the same file or directive
/// is the same <see cref="NginxFile"/> or <see cref="Directive"/> wherever it is reached. Kept here, in the public
/// layer: the internal objects do not know their views.
/// </summary>
internal static class Facades
{
    private static readonly ConditionalWeakTable<ConfigDocument, NginxFile> Files = new();
    private static readonly ConditionalWeakTable<StructureBlock, DirectiveCollection> Collections = new();
    private static readonly ConditionalWeakTable<StructureDirective, Directive> Directives = new();

    public static NginxFile Of(ConfigDocument document) => Files.GetValue(document, key => new NginxFile(key));

    public static DirectiveCollection Of(StructureBlock block) => Collections.GetValue(block, key => new DirectiveCollection(key));

    public static Directive Of(StructureDirective directive) => Directives.GetValue(directive, key => new Directive(key));
}
