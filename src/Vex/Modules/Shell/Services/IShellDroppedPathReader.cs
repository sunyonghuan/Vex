using Avalonia.Input;

namespace Vex.Modules.Shell.Services;

public interface IShellDroppedPathReader
{
    string? GetFirstLocalPath(DragEventArgs e);
}
