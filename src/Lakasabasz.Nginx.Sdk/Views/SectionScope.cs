using Lakasabasz.Nginx.Sdk.Internal.Structure;

namespace Lakasabasz.Nginx.Sdk.Views;

/// <summary>
/// Where a collection of sections lives: the directives its sections are found among and the block new ones go to.
/// Read anew on each use, so a block added later (e.g. <c>http</c> in a new file) counts at once.
/// </summary>
internal abstract class SectionScope
{
    /// <summary>The directives the sections are found among, in the order of the file.</summary>
    public abstract IEnumerable<StructureDirective> Directives { get; }

    /// <summary>The block new sections are added to.</summary>
    public abstract StructureBlock Target { get; }

    /// <summary>The sections of one block, e.g. the locations of a server.</summary>
    public static SectionScope Of(StructureBlock block) => new BlockScope(block);

    /// <summary>
    /// The HTTP sections of a whole file: at its top level (a site, a file of <c>conf.d</c>) and inside its <c>http</c>
    /// block (<c>nginx.conf</c>), in place of the <c>http</c> directive itself. New ones go into <c>http</c> when the
    /// file has it, else to the top level. nginx allows one <c>http</c> block, so the first one is where they go.
    /// </summary>
    public static SectionScope OfFile(StructureBlock root) => new FileScope(root);

    private sealed class BlockScope(StructureBlock block) : SectionScope
    {
        public override IEnumerable<StructureDirective> Directives => block.Directives;

        public override StructureBlock Target => block;
    }

    private sealed class FileScope(StructureBlock root) : SectionScope
    {
        public override IEnumerable<StructureDirective> Directives => root.Directives
            .SelectMany(directive => IsHttp(directive) ? directive.Block.Directives : [directive]);

        public override StructureBlock Target => root.Directives.FirstOrDefault(IsHttp)?.Block ?? root;

        private static bool IsHttp(StructureDirective directive) =>
            directive.Name == Http.Keyword && directive.Kind == DirectiveKind.Block;
    }
}
