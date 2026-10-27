using CodeWF.Toolkit.EventBus;

namespace Netor.Markdor.Core.Messaging;

public sealed class ShellDroppedPathCommand : Command
{
    public ShellDroppedPathCommand(string path)
    {
        Path = path;
    }

    public string Path { get; }
}
