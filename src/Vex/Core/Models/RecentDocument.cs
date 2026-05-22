namespace Vex.Core.Models;

public sealed record RecentDocument(string Path)
{
    public string FileName => System.IO.Path.GetFileName(Path);

    public string FolderName => System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(Path)) ?? string.Empty;

    public string DisplayText => string.IsNullOrWhiteSpace(FolderName)
        ? FileName
        : $"{FileName} - {FolderName}";
}
