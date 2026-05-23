using System.Text.RegularExpressions;
using Vex.Core.Models;
using Vex.Core.Services;

namespace Vex.Modules.Workspace.Services;

public sealed partial class MarkdownStatisticsService : IMarkdownStatisticsService
{
    public MarkdownStatistics Count(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return new MarkdownStatistics(0, 0, 1);
        }

        // 统计前先弱化常见 Markdown 标记，避免 #、*、链接符号等语法字符污染正文指标。
        var text = MarkdownSyntaxRegex().Replace(markdown, " ");
        var latinWords = LatinWordRegex().Matches(text).Count;
        var cjkWords = CjkRegex().Matches(text).Count;
        var words = latinWords + cjkWords;
        var characters = text.Count(c => !char.IsWhiteSpace(c));
        var lineMetrics = CountLineMetrics(markdown);
        var readingMinutes = words == 0 ? 0 : Math.Max(1, (int)Math.Ceiling(words / 220d));
        return new MarkdownStatistics(
            words,
            characters,
            lineMetrics.Lines,
            lineMetrics.Paragraphs,
            lineMetrics.Headings,
            readingMinutes);
    }

    private static LineMetrics CountLineMetrics(string markdown)
    {
        var lines = CountLines(markdown);
        var paragraphs = 0;
        var headings = 0;
        var inParagraph = false;
        using var reader = new StringReader(markdown);

        while (reader.ReadLine() is { } rawLine)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line))
            {
                if (inParagraph)
                {
                    paragraphs++;
                    inParagraph = false;
                }

                continue;
            }

            if (HeadingRegex().IsMatch(line))
            {
                headings++;
                continue;
            }

            if (!HorizontalRuleRegex().IsMatch(line))
            {
                inParagraph = true;
            }
        }

        if (inParagraph)
        {
            paragraphs++;
        }

        return new LineMetrics(lines, paragraphs, headings);
    }

    private static int CountLines(string markdown)
    {
        var lines = 1;
        for (var i = 0; i < markdown.Length; i++)
        {
            if (markdown[i] == '\n')
            {
                lines++;
            }
            else if (markdown[i] == '\r' && (i + 1 >= markdown.Length || markdown[i + 1] != '\n'))
            {
                lines++;
            }
        }

        return lines;
    }

    private readonly record struct LineMetrics(int Lines, int Paragraphs, int Headings);

    [GeneratedRegex(@"[`*_>#\-\[\]\(\)!|]")]
    private static partial Regex MarkdownSyntaxRegex();

    [GeneratedRegex(@"[\p{L}\p{N}]+", RegexOptions.CultureInvariant)]
    private static partial Regex LatinWordRegex();

    [GeneratedRegex(@"[\u3400-\u9FFF]")]
    private static partial Regex CjkRegex();

    [GeneratedRegex(@"^#{1,6}\s+\S+", RegexOptions.Multiline)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^(-{3,}|\*{3,}|_{3,})$")]
    private static partial Regex HorizontalRuleRegex();
}
