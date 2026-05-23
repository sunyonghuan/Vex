namespace Vex.Core.Services;

public interface IWorkspaceDocumentState
{
    string Markdown { get; }

    string? FilePath { get; }

    void UpdateDocument(string markdown, string? filePath);
}
