using CodeWF.Toolkit.EventBus;

namespace Netor.Markdor.Core.Messaging;

public sealed class EditorActionCommand : Command
{
    public EditorActionCommand(EditorActionKind action)
    {
        Action = action;
    }

    public EditorActionKind Action { get; }
}
