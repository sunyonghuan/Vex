using System.Diagnostics;
using Netor.Markdor.Core.Services;

namespace Netor.Markdor.Modules.Shell.Services;

public sealed class ShellProcessLauncher : IDocumentWindowLauncher
{
    public void Open(string path)
    {
        using var process = Process.Start(CreateStartInfo(path))
            ?? throw new InvalidOperationException("Cannot start the document window.");
    }

    public static ProcessStartInfo CreateStartInfo(string? path = null)
    {
        var executable = Environment.ProcessPath ?? throw new InvalidOperationException("Application executable is unavailable.");
        var start = new ProcessStartInfo(executable) { UseShellExecute = false };
        if (Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            start.ArgumentList.Add(typeof(Markdor.App).Assembly.Location);
        if (!string.IsNullOrWhiteSpace(path)) start.ArgumentList.Add(path);
        return start;
    }
}
