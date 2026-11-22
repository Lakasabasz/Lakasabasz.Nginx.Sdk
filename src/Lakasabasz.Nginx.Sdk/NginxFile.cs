using System.ComponentModel;
using Lakasabasz.Nginx.Sdk.Internal;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>A loaded config file: its directives, with every change tracked against the text it was loaded from.</summary>
/// <remarks>
/// A view over two internal parts: the file itself (path, text as stored) and the directives read from it. Writing
/// keeps the text of everything unchanged and formats the new text like its neighbours.
/// </remarks>
public sealed class NginxFile
{
    private readonly ConfigDocument _document;

    internal NginxFile(ConfigDocument document)
    {
        _document = document;
    }

    /// <summary>Reads and parses one config file; its includes are not followed.</summary>
    /// <param name="path">The file; links are resolved, a relative path starts at the current directory.</param>
    /// <returns>The file, not part of any <see cref="Nginx"/>.</returns>
    /// <exception cref="NginxParseException">The file does not exist or has a syntax error.</exception>
    public static NginxFile Load(NginxPath path) => Facades.Of(ConfigDocument.Read(path).OrThrow());

    /// <summary>
    /// Full path of the file, with links resolved: a site loaded through its link in <c>sites-enabled</c> has the path
    /// of its file in <c>sites-available</c>.
    /// </summary>
    public NginxPath Path => _document.Path;

    /// <summary>The top-level directives of the file, read and changed by name: <c>Add</c>, <c>Set</c>, <c>Remove</c>, <c>AddBlock</c>.</summary>
    public DirectiveCollection Directives => Facades.Of(_document.Tree.Structure);

    /// <summary>
    /// The content of the file with the changes made so far, as it would be written. An unchanged file gives exactly
    /// the text it was loaded from.
    /// </summary>
    /// <remarks>
    /// Temporary: lets the tests check the serializer until writing changes (<c>Apply</c>, <c>CommitChanges</c>)
    /// exists; it will then be removed.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public string Serialize() => _document.Render();
}
