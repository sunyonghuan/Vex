using CodeWF.EventBus;

namespace Vex.Core.Messaging;

public sealed class EditorSearchCommand : Command
{
    public EditorSearchCommand(EditorSearchAction action, string searchText, string? replacementText = null)
    {
        Action = action;
        SearchText = searchText;
        ReplacementText = replacementText ?? string.Empty;
    }

    public EditorSearchAction Action { get; }

    public string SearchText { get; }

    public string ReplacementText { get; }
}
