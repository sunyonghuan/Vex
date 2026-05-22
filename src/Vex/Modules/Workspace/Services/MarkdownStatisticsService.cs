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

        var text = MarkdownSyntaxRegex().Replace(markdown, " ");
        var latinWords = LatinWordRegex().Matches(text).Count;
        var cjkWords = CjkRegex().Matches(text).Count;
        var characters = text.Count(c => !char.IsWhiteSpace(c));
        var lines = markdown.ReplaceLineEndings("\n").Split('\n').Length;
        return new MarkdownStatistics(latinWords + cjkWords, characters, lines);
    }

    [GeneratedRegex(@"[`*_>#\-\[\]\(\)!|]")]
    private static partial Regex MarkdownSyntaxRegex();

    [GeneratedRegex(@"[\p{L}\p{N}]+", RegexOptions.CultureInvariant)]
    private static partial Regex LatinWordRegex();

    [GeneratedRegex(@"[\u3400-\u9FFF]")]
    private static partial Regex CjkRegex();
}
