using CodeWF.Toolkit.EventBus;

namespace Netor.Markdor.Core.Messaging;

public sealed class ShellActionCommand : Command
{
    public ShellActionCommand(ShellActionKind action, string? parameter = null)
    {
        Action = action;
        Parameter = parameter;
    }

    public ShellActionKind Action { get; }

    public string? Parameter { get; }
}
