using System.Runtime.CompilerServices;
using System.Text;

namespace Lakasabasz.Nginx.Sdk.Tests.Infrastructure;

public sealed record SourceLiteral(string File, int Line, string Text, int Start);

public sealed record SourceCall(string File, int Line, string Method, int Arguments, int MessageStart, int MessageEnd);

public static class SourceScanner
{
    private static readonly string[] AssertMethods =
    [
        Consts.Source.AssertThat, Consts.Source.AssumeThat, Consts.Source.AssertThrows, Consts.Source.EventuallyAsync,
    ];

    public static IEnumerable<string> ProjectFiles([CallerFilePath] string? callerFile = null)
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(callerFile!)!);
        while (!File.Exists(Path.Combine(directory.FullName, Consts.Source.ProjectFile)))
            directory = directory.Parent!;

        return Directory.EnumerateFiles(directory.FullName, Consts.Source.Pattern, SearchOption.AllDirectories)
            .Where(file => !Path.GetRelativePath(directory.FullName, file).Split(Path.DirectorySeparatorChar)
                .Any(part => part is Consts.Source.Obj or Consts.Source.Bin))
            .Order(StringComparer.Ordinal);
    }

    public static IReadOnlyList<SourceLiteral> DisallowedLiterals(string file)
    {
        var (code, literals) = Scan(file);
        var allowed = AssertCalls(file, code).Select(call => (call.MessageStart, call.MessageEnd))
            .Concat(Calls(code, Consts.Source.DescriptionAttribute).Select(open => (open, Arguments(code, open)[^1].End)))
            .ToList();
        return literals.Where(literal => !allowed.Any(span => literal.Start >= span.Item1 && literal.Start < span.Item2)).ToList();
    }

    public static IReadOnlyList<SourceCall> AssertCalls(string file) => AssertCalls(file, Scan(file).Code);

    private static List<SourceCall> AssertCalls(string file, string code) => AssertMethods
        .SelectMany(method => Calls(code, method).Select(open =>
        {
            var arguments = Arguments(code, open);
            var message = arguments[method == Consts.Source.EventuallyAsync ? Math.Min(1, arguments.Count - 1) : ^1];
            return new SourceCall(file, LineOf(code, open), method, arguments.Count, message.Start, message.End);
        }))
        .ToList();

    private static IEnumerable<int> Calls(string code, string method)
    {
        for (var at = code.IndexOf(method, StringComparison.Ordinal); at >= 0; at = code.IndexOf(method, at + 1, StringComparison.Ordinal))
            yield return code.IndexOf(Consts.Source.OpenParenthesis, at + method.Length - 1);
    }

    private static (string Code, List<SourceLiteral> Literals) Scan(string file)
    {
        var text = File.ReadAllText(file);
        var code = new StringBuilder(text.Length);
        var literals = new List<SourceLiteral>();
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            var next = i + 1 < text.Length ? text[i + 1] : default;
            var end = c == Consts.Source.Slash && next == Consts.Source.Slash ? LineEnd(text, i)
                : c == Consts.Source.Slash && next == Consts.Source.Star ? BlockCommentEnd(text, i)
                : (int?)null;
            if (end is null && LiteralEnd(text, i) is { } literalEnd)
            {
                literals.Add(new SourceLiteral(file, LineOf(text, i), text[i..literalEnd], i));
                end = literalEnd;
            }
            if (end is { } blankedEnd)
            {
                foreach (var blanked in text.AsSpan(i, blankedEnd - i))
                    code.Append(blanked == Consts.Source.NewLine ? Consts.Source.NewLine : Consts.Source.Blank);
                i = blankedEnd;
                continue;
            }
            code.Append(c);
            i++;
        }
        return (code.ToString(), literals);
    }

    private static int LineEnd(string text, int start)
    {
        var end = text.IndexOf(Consts.Source.NewLine, start);
        return end < 0 ? text.Length : end;
    }

    private static int BlockCommentEnd(string text, int start)
    {
        var end = text.IndexOf(Consts.Source.BlockCommentEnd, start + 2, StringComparison.Ordinal);
        return end < 0 ? text.Length : end + Consts.Source.BlockCommentEnd.Length;
    }

    private static int? LiteralEnd(string text, int start)
    {
        var i = start;
        var verbatim = false;
        while (i < text.Length && (text[i] == Consts.Source.Dollar || text[i] == Consts.Source.At))
        {
            verbatim |= text[i] == Consts.Source.At;
            i++;
        }
        if (i >= text.Length)
            return null;

        if (text[i] == Consts.Source.Apostrophe && i == start)
        {
            var end = i + 1;
            while (end < text.Length && text[end] != Consts.Source.Apostrophe)
                end += text[end] == Consts.Source.Backslash ? 2 : 1;
            return end + 1;
        }
        if (text[i] != Consts.Source.Quote)
            return null;

        if (string.CompareOrdinal(text, i, Consts.Source.RawQuotes, 0, Consts.Source.RawQuotes.Length) == 0)
        {
            var end = text.IndexOf(Consts.Source.RawQuotes, i + Consts.Source.RawQuotes.Length, StringComparison.Ordinal);
            return end < 0 ? text.Length : end + Consts.Source.RawQuotes.Length;
        }

        var j = i + 1;
        while (j < text.Length)
        {
            if (verbatim && text[j] == Consts.Source.Quote && j + 1 < text.Length && text[j + 1] == Consts.Source.Quote)
                j += 2;
            else if (!verbatim && text[j] == Consts.Source.Backslash)
                j += 2;
            else if (text[j] == Consts.Source.Quote)
                return j + 1;
            else
                j++;
        }
        return text.Length;
    }

    private static List<(int Start, int End)> Arguments(string code, int open)
    {
        var arguments = new List<(int Start, int End)>();
        var depth = 0;
        var start = open + 1;
        for (var i = open; i < code.Length; i++)
        {
            switch (code[i])
            {
                case Consts.Source.OpenParenthesis or Consts.Source.OpenBracket or Consts.Source.OpenBrace:
                    depth++;
                    break;
                case Consts.Source.CloseParenthesis or Consts.Source.CloseBracket or Consts.Source.CloseBrace:
                    if (--depth == 0)
                    {
                        arguments.Add((start, i));
                        return arguments;
                    }
                    break;
                case Consts.Source.Comma when depth == 1:
                    arguments.Add((start, i));
                    start = i + 1;
                    break;
            }
        }
        arguments.Add((start, code.Length));
        return arguments;
    }

    private static int LineOf(string text, int index) => text.AsSpan(0, index).Count(Consts.Source.NewLine) + 1;
}
