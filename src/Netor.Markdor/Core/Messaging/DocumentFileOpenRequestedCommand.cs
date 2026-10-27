using CodeWF.Toolkit.EventBus;
using Netor.Markdor.Core.Models;

namespace Netor.Markdor.Core.Messaging;

public sealed class DocumentFileOpenRequestedCommand : Command
{
    public DocumentFileOpenRequestedCommand(DocumentFile file, DocumentFile? previousSelection)
    {
        File = file;
        PreviousSelection = previousSelection;
    }

    public DocumentFile File { get; }

    public DocumentFile? PreviousSelection { get; }
}
