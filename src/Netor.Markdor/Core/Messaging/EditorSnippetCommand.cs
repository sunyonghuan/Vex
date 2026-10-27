using CodeWF.Toolkit.EventBus;

namespace Netor.Markdor.Core.Messaging;

public sealed class EditorSnippetCommand(string key) : Command
{
    public string Key { get; } = key;
}
