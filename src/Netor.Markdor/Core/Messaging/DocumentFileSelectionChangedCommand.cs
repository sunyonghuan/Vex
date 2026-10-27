using CodeWF.Toolkit.EventBus;
using Netor.Markdor.Core.Models;

namespace Netor.Markdor.Core.Messaging;

public sealed class DocumentFileSelectionChangedCommand : Command
{
    public DocumentFileSelectionChangedCommand(DocumentFile? selectedFile, bool revealPath = false)
    {
        SelectedFile = selectedFile;
        RevealPath = revealPath;
    }

    public DocumentFile? SelectedFile { get; }

    public bool RevealPath { get; }
}
