using System.Text;
using Lakasabasz.Nginx.Sdk.Internal;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>
/// One directive of a config file, e.g. <c>listen 80;</c>, or a block directive such as <c>server { … }</c>.
/// </summary>
/// <remarks>
/// A directive loaded from a file keeps its original text: it is written back exactly as it was until its
/// arguments change. A raw block (e.g. <c>content_by_lua_block { … }</c>) keeps its body as opaque text in
/// <see cref="RawContent"/>; it is never parsed.
/// </remarks>
public sealed class Directive
{
    private readonly string? _originalHeader;
    private IReadOnlyList<string> _arguments;
    private bool _changed;

    internal Directive(string name, IReadOnlyList<string> arguments, DirectiveKind kind, string? originalHeader,
        int line, int column, DirectiveCollection? children, string? rawContent)
    {
        Name = name;
        _arguments = arguments;
        Kind = kind;
        _originalHeader = originalHeader;
        Line = line;
        Column = column;
        Directives = children ?? DirectiveCollection.NotABlock;
        RawContent = rawContent;
        Directives.Owner = this;
    }

    /// <summary>The directive's name, e.g. <c>listen</c>.</summary>
    public string Name { get; }

    /// <summary>The arguments, without quotes and with escapes resolved, as nginx reads them.</summary>
    public IReadOnlyList<string> Arguments => _arguments;

    /// <summary>Whether this is a block (<c>name … { … }</c>), including raw blocks.</summary>
    public bool IsBlock => Kind != DirectiveKind.Simple;

    /// <summary>The directives inside the block. Empty, and not changeable, for a simple directive or a raw block.</summary>
    public DirectiveCollection Directives { get; }

    /// <summary>The body of a raw block (e.g. Lua code) exactly as written; <see langword="null"/> otherwise.</summary>
    public string? RawContent { get; }

    /// <summary>1-based line where the directive starts in the loaded file; 0 for a directive added in code.</summary>
    /// <remarks>Refers to the text as loaded: it does not follow later changes of the file.</remarks>
    public int Line { get; }

    /// <summary>1-based column where the directive starts in the loaded file; 0 for a directive added in code.</summary>
    public int Column { get; }

    /// <summary>The collection the directive is in, or <see langword="null"/> after it was removed.</summary>
    internal DirectiveCollection? Parent { get; set; }

    internal DirectiveKind Kind { get; }

    internal void SetArguments(IReadOnlyList<string> arguments)
    {
        _arguments = arguments;
        _changed = true;
    }

    internal void Render(StringBuilder output)
    {
        if (_originalHeader is not null && !_changed)
            output.Append(_originalHeader);
        else
            ConfigWriter.AppendHeader(output, Name, _arguments, IsBlock);

        switch (Kind)
        {
            case DirectiveKind.Simple:
                output.Append(';');
                break;
            case DirectiveKind.Raw:
                output.Append('{').Append(RawContent).Append('}');
                break;
            default:
                output.Append('{');
                Directives.Render(output);
                output.Append('}');
                break;
        }
    }
}

internal enum DirectiveKind
{
    Simple,
    Block,
    Raw,
}
