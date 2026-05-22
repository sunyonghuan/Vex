using CodeWF.EventBus;

namespace Vex.Core.Messaging;

public sealed class EditorSearchResultCommand : Command
{
    public EditorSearchResultCommand(string message)
    {
        Message = message;
    }

    public string Message { get; }
}
