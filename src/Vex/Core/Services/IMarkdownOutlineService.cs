using Vex.Core.Models;

namespace Vex.Core.Services;

public interface IMarkdownOutlineService
{
    IReadOnlyList<OutlineItem> BuildOutline(string markdown);
}
