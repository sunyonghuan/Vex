using CodeWF.Toolkit.EventBus;

namespace Netor.Markdor.Core.Messaging;

public sealed class ShellSidebarTabSelectedCommand : Command
{
    public ShellSidebarTabSelectedCommand(int selectedIndex)
    {
        SelectedIndex = Math.Max(0, selectedIndex);
    }

    public int SelectedIndex { get; }
}
