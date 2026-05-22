using System.Text.RegularExpressions;
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
        var lines = markdown.ReplaceLineEndings("\n").Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                inFence = !inFence;
                continue;
            }

            if (inFence)
            {
                continue;
            }

            var match = HeadingRegex().Match(line);
            if (!match.Success)
            {
                continue;
            }

            var level = match.Groups["level"].Value.Length;
            var title = match.Groups["title"].Value.Trim();
            if (!string.IsNullOrWhiteSpace(title))
            {
                result.Add(new OutlineItem(level, title, i + 1));
            }
        }

        return result;
    }

    [GeneratedRegex("^(?<level>#{1,6})\\s+(?<title>.+?)\\s*$")]
    private static partial Regex HeadingRegex();
}
