using Lakasabasz.Nginx.Sdk.Internal.Syntax;

namespace Lakasabasz.Nginx.Sdk.Internal;

/// <summary>
/// One config file: its path, how its text is stored (encoding, BOM) and its syntax tree. It gives the tree the text
/// it read and takes the text back to write; it knows nothing about the directives in it.
/// </summary>
internal sealed class ConfigDocument
{
    private readonly ConfigSource _source;

    private ConfigDocument(NginxPath path, ConfigSource source, SyntaxTree tree)
    {
        Path = path;
        _source = source;
        Tree = tree;
    }

    /// <summary>The real path of the file, with links resolved.</summary>
    public NginxPath Path { get; }

    public SyntaxTree Tree { get; }

    /// <summary>Reads and parses one file; a missing or broken file is an expected result when loading many.</summary>
    /// <param name="path">The file; links are resolved, a relative path starts at the current directory.</param>
    public static Result<ConfigDocument, NginxParseError> Read(NginxPath path)
    {
        if (!path.TryResolveLinks(out var realPath) || realPath.IsDirectory)
            return Result<ConfigDocument, NginxParseError>.Failure(NginxParseError.NotFound(path));

        var source = ConfigSource.Read(realPath);
        var parsed = ConfigParser.Parse(realPath, source.Text);
        return parsed.IsSuccess
            ? Result<ConfigDocument, NginxParseError>.Success(new ConfigDocument(realPath, source, parsed.Value))
            : Result<ConfigDocument, NginxParseError>.Failure(parsed.Error);
    }

    /// <summary><see cref="Read"/> that throws: the file does not exist or has a syntax error.</summary>
    /// <exception cref="NginxParseException">The file does not exist or has a syntax error.</exception>
    public static ConfigDocument Load(NginxPath path)
    {
        var result = Read(path);
        return result.IsSuccess ? result.Value : throw new NginxParseException([result.Error]);
    }

    /// <summary>A new, empty file, not on disk yet.</summary>
    public static ConfigDocument Empty(NginxPath path) =>
        new(path, ConfigSource.Empty, ConfigParser.Parse(path, ConfigSource.Empty.Text).Value!);

    /// <summary>The text of the file with the changes made so far, as it would be written.</summary>
    public string Render() => Tree.Render(_source.Text.Length);
}
