using Vex.Core.Models;

namespace Vex.Modules.Shell.Services;

public interface IAutoSaveDraftService
{
    DocumentSnapshot? TryRestore(DocumentSnapshot document);

    void QueueSave(DocumentSnapshot document, string markdown, string lastSavedMarkdown);

    void Clear(DocumentSnapshot document);

    void Flush();
}
