using Vex.Core.Models;
using Vex.Modules.Shell.ViewModels;

namespace Vex.Modules.Shell.Services;

public interface IShellDocumentUtilityActions
{
    void ShowProperties(ShellDocumentInfoViewModel documentInfo);

    Task ExportAsync(DocumentSnapshot document, string markdown, string? format);

    Task CopyHtmlAsync(DocumentSnapshot document, string markdown, string? target);

    Task PrintAsync(DocumentSnapshot document, string markdown);

    void WordCount(ShellDocumentInfoViewModel documentInfo);
}
