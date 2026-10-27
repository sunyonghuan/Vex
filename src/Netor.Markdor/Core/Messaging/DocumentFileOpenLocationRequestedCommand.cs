using CodeWF.Toolkit.EventBus;
using Netor.Markdor.Core.Models;

namespace Netor.Markdor.Core.Messaging;

public sealed class DocumentFileOpenLocationRequestedCommand : Command
{
    public DocumentFileOpenLocationRequestedCommand(DocumentFile file)
    {
        File = file;
    }

    public DocumentFile File { get; }
}
