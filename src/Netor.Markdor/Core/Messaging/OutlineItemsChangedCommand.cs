using CodeWF.Toolkit.EventBus;
using Netor.Markdor.Core.Models;

namespace Netor.Markdor.Core.Messaging;

public sealed class OutlineItemsChangedCommand : Command
{
    public OutlineItemsChangedCommand(IEnumerable<OutlineItem> items)
    {
        Items = items.ToArray();
    }

    public IReadOnlyList<OutlineItem> Items { get; }
}
