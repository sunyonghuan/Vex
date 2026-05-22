using Avalonia.Input;
using Avalonia.Platform.Storage;

namespace Vex.Modules.Shell.Services;

public sealed class ShellDroppedPathReader : IShellDroppedPathReader
{
    public string? GetFirstLocalPath(DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles();
        if (files is null)
        {
            return null;
        }

        foreach (var item in files)
        {
            var path = item.TryGetLocalPath();
            if (IsAvailableLocalPath(path))
            {
                return path;
            }
        }

        return null;
    }

    private static bool IsAvailableLocalPath(string? path)
    {
        // 拖放事件可能包含虚拟文件或空路径，先只接收可由本地文件系统访问的项目。
        return !string.IsNullOrWhiteSpace(path) && (File.Exists(path) || Directory.Exists(path));
    }
}
