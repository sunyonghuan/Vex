using Vex.Core.Models;
using Vex.Core.Services;

namespace Vex.Modules.Workspace.Services;

public sealed class MarkdownStatisticsService : IMarkdownStatisticsService
{
    public MarkdownStatistics Count(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return new MarkdownStatistics(0, 0, 1);
        }

        var textMetrics = CountTextMetrics(markdown);
        var lineMetrics = CountLineMetrics(markdown);
        var readingMinutes = textMetrics.Words == 0
            ? 0
            : Math.Max(1, (int)Math.Ceiling(textMetrics.Words / 220d));

        return new MarkdownStatistics(
            textMetrics.Words,
            textMetrics.Characters,
            lineMetrics.Lines,
            lineMetrics.Paragraphs,
            lineMetrics.Headings,
            readingMinutes);
    }

    private static TextMetrics CountTextMetrics(string markdown)
    {
        var words = 0;
        var characters = 0;
        var inLatinWord = false;

        foreach (var character in markdown)
        {
            if (char.IsWhiteSpace(character) || IsMarkdownSyntaxCharacter(character))
            {
                inLatinWord = false;
                continue;
            }

            characters++;

            if (IsCjkCharacter(character))
            {
                words++;
                inLatinWord = false;
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                if (!inLatinWord)
                {
                    words++;
                    inLatinWord = true;
                }

                continue;
            }

            inLatinWord = false;
        }

        return new TextMetrics(words, characters);
    }

    private static LineMetrics CountLineMetrics(string markdown)
    {
        var lines = 1;
        var paragraphs = 0;
        var headings = 0;
        var inParagraph = false;
        var lineStart = 0;

        for (var index = 0; index <= markdown.Length; index++)
        {
            var isEnd = index == markdown.Length;
            if (!isEnd && markdown[index] != '\r' && markdown[index] != '\n')
            {
                continue;
            }

            var line = markdown.AsSpan(lineStart, index - lineStart).Trim();
            if (line.IsEmpty)
            {
                if (inParagraph)
                {
                    paragraphs++;
                    inParagraph = false;
                }
            }
            else if (IsHeading(line))
            {
                headings++;
            }
            else if (!IsHorizontalRule(line))
            {
                inParagraph = true;
            }

            if (!isEnd)
            {
                lines++;
                if (markdown[index] == '\r' && index + 1 < markdown.Length && markdown[index + 1] == '\n')
                {
                    index++;
                }

                lineStart = index + 1;
            }
        }

        if (inParagraph)
        {
            paragraphs++;
        }

        return new LineMetrics(lines, paragraphs, headings);
    }

    private static bool IsHeading(ReadOnlySpan<char> line)
    {
        var level = 0;
        while (level < line.Length && level < 6 && line[level] == '#')
        {
            level++;
        }

        if (level == 0 || level >= line.Length || !char.IsWhiteSpace(line[level]))
        {
            return false;
        }

        for (var index = level + 1; index < line.Length; index++)
        {
            if (!char.IsWhiteSpace(line[index]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsHorizontalRule(ReadOnlySpan<char> line)
    {
        if (line.Length < 3 || line[0] is not ('-' or '*' or '_'))
        {
            return false;
        }

        for (var index = 1; index < line.Length; index++)
        {
            if (line[index] != line[0])
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsMarkdownSyntaxCharacter(char character)
    {
        return character is '`' or '*' or '_' or '>' or '#' or '-' or '[' or ']' or '(' or ')' or '!' or '|';
    }

    private static bool IsCjkCharacter(char character)
    {
        return character is >= '\u3400' and <= '\u9FFF';
    }

    private readonly record struct TextMetrics(int Words, int Characters);

    private readonly record struct LineMetrics(int Lines, int Paragraphs, int Headings);
}
