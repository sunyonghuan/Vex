using Vex.Core.Models;
using Vex.Core.Services;

namespace Vex.Modules.Workspace.Services;

public sealed partial class MarkdownOutlineService : IMarkdownOutlineService
{
    public IReadOnlyList<OutlineItem> BuildOutline(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return [];
        }

        var result = new List<OutlineItem>();
        var inFence = false;
        var text = markdown.AsSpan();
        var lineNumber = 0;
        var lineStart = 0;
        while (lineStart < text.Length)
        {
            var lineEnd = FindLineEnd(text, lineStart);
            var line = text[lineStart..lineEnd];
            lineNumber++;
            if (IsFenceStart(line))
            {
                inFence = !inFence;
                lineStart = MoveToNextLine(text, lineEnd);
                continue;
            }

            if (inFence)
            {
                lineStart = MoveToNextLine(text, lineEnd);
                continue;
            }

            if (!TryParseHeading(line, out var level, out var title))
            {
                lineStart = MoveToNextLine(text, lineEnd);
                continue;
            }

            result.Add(new OutlineItem(level, title, lineNumber));
            lineStart = MoveToNextLine(text, lineEnd);
        }

        return result;
    }

    private static int FindLineEnd(ReadOnlySpan<char> text, int start)
    {
        var index = start;
        while (index < text.Length && text[index] is not '\r' and not '\n')
        {
            index++;
        }

        return index;
    }

    private static int MoveToNextLine(ReadOnlySpan<char> text, int lineEnd)
    {
        if (lineEnd >= text.Length)
        {
            return text.Length;
        }

        return text[lineEnd] == '\r' && lineEnd + 1 < text.Length && text[lineEnd + 1] == '\n'
            ? lineEnd + 2
            : lineEnd + 1;
    }

    private static bool IsFenceStart(ReadOnlySpan<char> line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith("```".AsSpan(), StringComparison.Ordinal)
               || trimmed.StartsWith("~~~".AsSpan(), StringComparison.Ordinal);
    }

    private static bool TryParseHeading(ReadOnlySpan<char> line, out int level, out string title)
    {
        level = 0;
        title = string.Empty;

        while (level < line.Length && level < 6 && line[level] == '#')
        {
            level++;
        }

        if (level == 0 || level >= line.Length || !char.IsWhiteSpace(line[level]))
        {
            return false;
        }

        var titleSpan = line[(level + 1)..].Trim();
        if (titleSpan.Length == 0)
        {
            return false;
        }

        title = titleSpan.ToString();
        return true;
    }
}
