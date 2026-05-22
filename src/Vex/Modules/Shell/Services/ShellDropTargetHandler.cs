using Avalonia.Input;
using CodeWF.EventBus;
using Vex.Core.Messaging;

namespace Vex.Modules.Shell.Services;

public sealed class ShellDropTargetHandler : IShellDropTargetHandler
{
    private readonly IShellDroppedPathReader _droppedPaths;
    private readonly IEventBus _eventBus;

    public ShellDropTargetHandler(IShellDroppedPathReader droppedPaths, IEventBus eventBus)
    {
        _droppedPaths = droppedPaths;
        _eventBus = eventBus;
    }

    public DragDropEffects GetDragEffects(DragEventArgs e)
    {
        return _droppedPaths.GetFirstLocalPath(e) is null
            ? DragDropEffects.None
            : DragDropEffects.Copy;
    }

    public void PublishDroppedPath(DragEventArgs e)
    {
        var path = _droppedPaths.GetFirstLocalPath(e);
        if (path is null)
        {
            return;
        }

        // 窗口只发布拖入路径，未保存确认和打开流程继续由 Shell 文档流程统一处理。
        _eventBus.Publish(new ShellDroppedPathCommand(path));
    }
}
