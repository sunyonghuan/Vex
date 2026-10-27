using Avalonia.Input;

namespace Netor.Markdor.Modules.Shell.Services;

public interface IShellDropTargetHandler
{
    DragDropEffects GetDragEffects(DragEventArgs e);

    void PublishDroppedPath(DragEventArgs e);
}
