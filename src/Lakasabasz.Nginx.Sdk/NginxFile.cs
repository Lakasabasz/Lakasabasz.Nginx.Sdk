using System.ComponentModel;
using System.Text;
using Lakasabasz.Nginx.Sdk.Internal;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>A loaded config file: its directives, with every change tracked against the text it was loaded from.</summary>
public sealed class NginxFile
{
    private readonly ConfigSource _source;

    internal NginxFile(string path, ConfigSource source, DirectiveCollection directives)
    {
        Path = path;
        _source = source;
        Directives = directives;
    }

    internal static NginxFile Empty(string path) =>
        new(path, ConfigSource.Empty, ConfigParser.Parse(path, ConfigSource.Empty.Text).Directives!);

    /// <summary>
    /// Full path of the file, with links resolved: a site loaded through its link in <c>sites-enabled</c> has the path
    /// of its file in <c>sites-available</c>.
    /// </summary>
    public string Path { get; }

    /// <summary>The top-level directives of the file, read and changed by name: <c>Add</c>, <c>Set</c>, <c>Remove</c>, <c>AddBlock</c>.</summary>
    public DirectiveCollection Directives { get; }

    /// <summary>
    /// The content of the file with the changes made so far, as it would be written. An unchanged file gives exactly
    /// the text it was loaded from.
    /// </summary>
    /// <remarks>
    /// Temporary: lets the tests check the serializer until writing changes (<c>Apply</c>, <c>CommitChanges</c>)
    /// exists; it will then be removed.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public string Serialize()
    {
        var text = new StringBuilder(_source.Text.Length);
        Directives.Render(text);
        return text.ToString();
    }
}
