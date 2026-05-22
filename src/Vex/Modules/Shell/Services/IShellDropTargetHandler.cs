using Avalonia.Input;

namespace Vex.Modules.Shell.Services;

public interface IShellDropTargetHandler
{
    DragDropEffects GetDragEffects(DragEventArgs e);

    void PublishDroppedPath(DragEventArgs e);
}
