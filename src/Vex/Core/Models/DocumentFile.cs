namespace Vex.Core.Models;

public sealed class DocumentFile
{
    public DocumentFile(string path, string name, string folderName, string modifiedText, string preview)
    {
        Path = path;
        Name = name;
        FolderName = folderName;
        ModifiedText = modifiedText;
        Preview = preview;
    }

    public string Name { get; }

    public string Path { get; }

    public string FolderName { get; }

    public string ModifiedText { get; }

    public string Preview { get; }
}
