using System.ComponentModel;
using Lakasabasz.Nginx.Sdk.Internal;
using Lakasabasz.Nginx.Sdk.Views;

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
    /// Every <c>server</c> block of the file: at its top level (a site) and in its <c>http</c> block (<c>nginx.conf</c>),
    /// in the order of the file. A new server goes into <c>http</c> when the file has one, else at the top level.
    /// </summary>
    public ServerCollection Servers => new(SectionScope.OfFile(_document.Tree.Structure));

    /// <summary>
    /// Every <c>upstream</c> block of the file: at its top level (a file of <c>conf.d</c>) and in its <c>http</c> block,
    /// in the order of the file. A new upstream goes into <c>http</c> when the file has one, else at the top level.
    /// </summary>
    public UpstreamCollection Upstreams => new(SectionScope.OfFile(_document.Tree.Structure));

    /// <summary>The <c>http { }</c> block, e.g. of <c>nginx.conf</c>; setting a loose one replaces it, <see langword="null"/> removes it.</summary>
    public Http? Http
    {
        get => Sections.Get(_document.Tree.Structure, Sdk.Http.Keyword, directive => new Http(directive));
        set => Sections.Set(_document.Tree.Structure, Sdk.Http.Keyword, value);
    }

    /// <summary>The <c>events { }</c> block of <c>nginx.conf</c>; setting a loose one replaces it, <see langword="null"/> removes it.</summary>
    public Events? Events
    {
        get => Sections.Get(_document.Tree.Structure, Sdk.Events.Keyword, directive => new Events(directive));
        set => Sections.Set(_document.Tree.Structure, Sdk.Events.Keyword, value);
    }

    /// <summary>
    /// How new and changed text of the file is written (see <see cref="NginxStyle"/>): read from the file, or
    /// <see cref="NginxStyle.Default"/> for a new one. Text that is not changed keeps its own.
    /// </summary>
    /// <exception cref="ArgumentException">The style is not usable, e.g. its line break is not <c>\n</c> or <c>\r\n</c>.</exception>
    public NginxStyle Style
    {
        get => _document.Tree.Structure.Style;
        set => _document.Tree.Structure.SetStyle(value).OrThrow();
    }

    /// <summary>
    /// The comment at the top of the file, without <c>#</c>, lines separated by <c>\n</c>; it is followed by one empty
    /// line. <see langword="null"/> for none; setting <see langword="null"/> removes it.
    /// </summary>
    /// <exception cref="ArgumentException">The comment contains a <c>\r</c>.</exception>
    public string? Comment
    {
        get => _document.Tree.Structure.Comment;
        set => _document.Tree.Structure.SetComment(value).OrThrow();
    }

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
