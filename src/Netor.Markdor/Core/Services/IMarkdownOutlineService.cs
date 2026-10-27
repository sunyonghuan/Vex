using Netor.Markdor.Core.Models;

namespace Netor.Markdor.Core.Services;

public interface IMarkdownOutlineService
{
    IReadOnlyList<OutlineItem> BuildOutline(string markdown);
}
