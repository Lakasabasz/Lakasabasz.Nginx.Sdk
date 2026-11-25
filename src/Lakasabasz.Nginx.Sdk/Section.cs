using Lakasabasz.Nginx.Sdk.Internal.Structure;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>
/// A block of a config file with a type of its own, e.g. <see cref="Server"/> or <see cref="Location"/>: its
/// directives are read and changed through typed properties.
/// </summary>
/// <remarks>
/// <para>A section made in code (<c>new Server()</c>) is loose: it belongs to no file. Adding it to a section
/// collection (<c>site.Servers.Add(server)</c>) puts this same object into the file, and later changes of it change the
/// file. A section is in at most one file or block at a time.</para>
/// <para>Properties read the directives every time, so a directive added through <see cref="Directives"/> or as nginx
/// text is read by the property at once. Setting a property to <see langword="null"/> removes its directive.</para>
/// </remarks>
public abstract class Section
{
    private protected Section(StructureDirective node)
    {
        Node = node;
    }

    private protected Section(string name, params string[] arguments)
        : this(StructureDirective.NewBlock(name, arguments, []))
    {
        Facades.Register(this);
    }

    /// <summary>The block directive itself (name, arguments, text from the file): the raw access to this section.</summary>
    public Directive Directive => Facades.Of(Node);

    /// <summary>The directives of the block, also those with no property, e.g. of a module outside nginx.</summary>
    public DirectiveCollection Directives => Facades.Of(Node.Block);

    internal StructureDirective Node { get; }

    internal StructureBlock Block => Node.Block;

    /// <summary>Removes this section from the file or block it is in.</summary>
    /// <exception cref="InvalidOperationException">The section is loose: it is in no file or block.</exception>
    public void Remove()
    {
        if (Node.Parent is not { } parent)
            throw new InvalidOperationException($"This {Node.Name} is in no file or block: there is nothing to remove it from.");
        parent.Remove(Node).OrThrow();
    }
}
