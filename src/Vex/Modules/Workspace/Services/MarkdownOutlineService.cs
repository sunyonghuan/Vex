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
        using var reader = new StringReader(markdown);

        var lineNumber = 0;
        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                inFence = !inFence;
                continue;
            }

            if (inFence)
            {
                continue;
            }

            if (!TryParseHeading(line, out var level, out var title))
            {
                continue;
            }

            result.Add(new OutlineItem(level, title, lineNumber));
        }

        return result;
    }

    private static bool TryParseHeading(string line, out int level, out string title)
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

        title = line[(level + 1)..].Trim();
        return title.Length > 0;
    }
}
