using Netor.Markdor.Core.Models;

namespace Netor.Markdor.Core.Services;

public interface IMarkdownStatisticsService
{
    MarkdownStatistics Count(string markdown);
}
