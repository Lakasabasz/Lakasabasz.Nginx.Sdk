using Lakasabasz.Nginx.Sdk.Internal.Structure;

namespace Lakasabasz.Nginx.Sdk;

/// <summary>
/// One directive of a config file, e.g. <c>listen 80;</c>, or a block directive such as <c>server { … }</c>.
/// </summary>
/// <remarks>
/// The raw access to any directive, also one with no typed property (e.g. of a module outside nginx): typed changes
/// of other directives leave it as it is. A directive loaded from a file keeps its original text: it is written back
/// exactly as it was until its arguments change. A raw block (e.g. <c>content_by_lua_block { … }</c>) keeps its body as opaque text in
/// <see cref="RawContent"/>; it is never parsed.
/// </remarks>
public sealed class Directive
{
    internal Directive(StructureDirective node)
    {
        Node = node;
    }

    /// <summary>The directive's name, e.g. <c>listen</c>.</summary>
    public string Name => Node.Name;

    /// <summary>The arguments, without quotes and with escapes resolved, as nginx reads them.</summary>
    public IReadOnlyList<string> Arguments => Node.Arguments;

    /// <summary>Whether this is a block (<c>name … { … }</c>), including raw blocks.</summary>
    public bool IsBlock => Node.IsBlock;

    /// <summary>The directives inside the block. Empty, and not changeable, for a simple directive or a raw block.</summary>
    public DirectiveCollection Directives => Facades.Of(Node.Block);

    /// <summary>The body of a raw block (e.g. Lua code) exactly as written; <see langword="null"/> otherwise.</summary>
    public string? RawContent => Node.RawContent;

    /// <summary>
    /// The comment at the end of the directive's line, without <c>#</c> (and the space after it), e.g. <c>listen 80; # public</c>;
    /// for a block, after its closing <c>}</c>. <see langword="null"/> for none; setting <see langword="null"/> removes it.
    /// </summary>
    /// <exception cref="ArgumentException">The comment contains a line break.</exception>
    public string? Comment
    {
        get => Node.Comment;
        set => Node.SetComment(value).OrThrow();
    }

    /// <summary>
    /// The directive as written in the file when it was loaded (for a block: up to its closing <c>}</c>);
    /// <see langword="null"/> for a directive added in code. Does not follow later changes.
    /// </summary>
    public string? Text => Node.SourceText;

    /// <summary>How the block is written: on one line, or with <c>{</c> on a line of its own (see <see cref="BlockLayout"/>).</summary>
    /// <exception cref="InvalidOperationException">Set on a simple directive or a raw block.</exception>
    public BlockLayout Layout
    {
        get => new() { OneLine = Node.Style.OneLine, BraceOnNewLine = Node.Style.BraceOnNewLine };
        set
        {
            if (Node.Kind != DirectiveKind.Block)
                throw new InvalidOperationException($"{Node.Name} is not a block: only a block has a layout.");
            Node.SetStyle(new BlockStyle(value.OneLine, value.BraceOnNewLine));
        }
    }

    /// <summary>1-based line where the directive starts in the loaded file; 0 for a directive added in code.</summary>
    /// <remarks>Refers to the text as loaded: it does not follow later changes of the file.</remarks>
    public int Line => Node.Line;

    /// <summary>1-based column where the directive starts in the loaded file; 0 for a directive added in code.</summary>
    public int Column => Node.Column;

    internal StructureDirective Node { get; }
}
