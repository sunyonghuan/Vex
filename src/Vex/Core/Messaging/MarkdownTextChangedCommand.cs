using CodeWF.EventBus;

namespace Vex.Core.Messaging;

public sealed class MarkdownTextChangedCommand : Command
{
    public MarkdownTextChangedCommand(string markdown, int caretLine, int caretColumn)
    {
        Markdown = markdown;
        CaretLine = caretLine;
        CaretColumn = caretColumn;
    }

    public string Markdown { get; }

    public int CaretLine { get; }

    public int CaretColumn { get; }
}
