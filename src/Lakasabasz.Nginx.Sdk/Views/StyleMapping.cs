using Lakasabasz.Nginx.Sdk.Internal;

namespace Lakasabasz.Nginx.Sdk.Views;

/// <summary>The public style and its internal counterpart, translated at the facades only.</summary>
internal static class StyleMapping
{
    private const char Tab = '\t';

    public static NginxStyle ToPublic(this TextStyle style) => new()
    {
        LineBreak = style.LineBreak == TextStyle.Crlf ? LineBreak.CrLf : LineBreak.Lf,
        Indentation = style.Indentation[0] == Tab ? Indentation.Tabs(style.Indentation.Length) : Indentation.Spaces(style.Indentation.Length),
        Braces = style.Braces switch
        {
            Braces.NewLine => BraceStyle.NewLine,
            Braces.OneLine => BraceStyle.OneLine,
            _ => BraceStyle.EndOfLine,
        },
        BlockSpacing = style.BlockSpacing,
    };

    /// <summary>The internal style of a public one; an error for a value that is none of the known ones.</summary>
    public static Result<TextStyle, UsageError> ToInternal(this NginxStyle style)
    {
        var lineBreak = style.LineBreak switch
        {
            LineBreak.Lf => TextStyle.Lf,
            LineBreak.CrLf => TextStyle.Crlf,
            _ => null,
        };
        Braces? braces = style.Braces switch
        {
            BraceStyle.EndOfLine => Braces.EndOfLine,
            BraceStyle.NewLine => Braces.NewLine,
            BraceStyle.OneLine => Braces.OneLine,
            _ => null,
        };
        if (lineBreak is null || braces is null || style.Indentation is null)
            return Result<TextStyle, UsageError>.Failure(new UsageError(UsageErrorKind.Argument,
                "The style needs a known line break, an indentation and a known brace style.", "value"));
        var unit = new string(style.Indentation.Character == IndentCharacter.Tab ? Tab : ' ', style.Indentation.Width);
        return Result<TextStyle, UsageError>.Success(new TextStyle(lineBreak, unit, braces.Value, style.BlockSpacing));
    }
}
