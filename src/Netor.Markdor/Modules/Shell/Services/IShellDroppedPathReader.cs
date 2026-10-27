using Avalonia.Input;

namespace Netor.Markdor.Modules.Shell.Services;

public interface IShellDroppedPathReader
{
    string? GetFirstLocalFile(DragEventArgs e);
}
