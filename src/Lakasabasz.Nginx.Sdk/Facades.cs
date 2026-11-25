using System.Runtime.CompilerServices;
using Lakasabasz.Nginx.Sdk.Internal;
using Lakasabasz.Nginx.Sdk.Internal.Structure;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>
/// The public view of each internal object, made once and then always the same object, so the same file or directive
/// is the same <see cref="NginxFile"/>, <see cref="Directive"/> or <see cref="Section"/> wherever it is reached. Kept
/// here, in the public layer: the internal objects do not know their views.
/// </summary>
/// <remarks>A block directive has two views: the generic <see cref="Directive"/> and its section.</remarks>
internal static class Facades
{
    private static readonly ConditionalWeakTable<ConfigDocument, NginxFile> Files = new();
    private static readonly ConditionalWeakTable<StructureBlock, DirectiveCollection> Collections = new();
    private static readonly ConditionalWeakTable<StructureDirective, Directive> Directives = new();
    private static readonly ConditionalWeakTable<StructureDirective, Section> Sections = new();

    public static NginxFile Of(ConfigDocument document) => Files.GetValue(document, key => new NginxFile(key));

    public static DirectiveCollection Of(StructureBlock block) => Collections.GetValue(block, key => new DirectiveCollection(key));

    public static Directive Of(StructureDirective directive) => Directives.GetValue(directive, key => new Directive(key));

    /// <summary>The section of a block directive, made by <paramref name="create"/> the first time it is reached.</summary>
    public static T Of<T>(StructureDirective directive, Func<StructureDirective, T> create)
        where T : Section => (T)Sections.GetValue(directive, key => create(key));

    /// <summary>Remembers a section made in code (<c>new Server()</c>), so its directive is reached as this same object.</summary>
    public static void Register(Section section) => Sections.Add(section.Node, section);
}
