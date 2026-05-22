namespace Vex.Core.Models;

public sealed class DocumentFile
{
    public DocumentFile(string path, string? workspaceRoot = null)
    {
        Path = path;
        Name = System.IO.Path.GetFileName(path);
        FolderName = ResolveFolderName(path, workspaceRoot);
        ModifiedText = ResolveModifiedText(path);
        Preview = ResolvePreview(path);
    }

    public string Name { get; }

    public string Path { get; }

    public string FolderName { get; }

    public string ModifiedText { get; }

    public string Preview { get; }

    private static string ResolveFolderName(string path, string? workspaceRoot)
    {
        var directory = System.IO.Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return "Vex";
        }

        if (!string.IsNullOrWhiteSpace(workspaceRoot))
        {
            var relative = System.IO.Path.GetRelativePath(workspaceRoot, directory);
            if (!relative.StartsWith("..", StringComparison.Ordinal))
            {
                return relative == "." ? System.IO.Path.GetFileName(workspaceRoot) : relative;
            }
        }

        return System.IO.Path.GetFileName(directory);
    }

    private static string ResolveModifiedText(string path)
    {
        if (!File.Exists(path))
        {
            return string.Empty;
        }

        var age = DateTimeOffset.Now - File.GetLastWriteTime(path);
        return age.TotalDays switch
        {
            < 1 => File.GetLastWriteTime(path).ToString("HH:mm"),
            < 2 => "1 day ago",
            < 8 => $"{(int)age.TotalDays} days ago",
            _ => File.GetLastWriteTime(path).ToString("MMM dd")
        };
    }

    private static string ResolvePreview(string path)
    {
        if (!File.Exists(path))
        {
            return string.Empty;
        }

        foreach (var line in File.ReadLines(path).Take(8))
        {
            var trimmed = line.Trim().TrimStart('#', '-', '*', '>', ' ');
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                return trimmed.Length > 96 ? $"{trimmed[..96]}..." : trimmed;
            }
        }

        return string.Empty;
    }
}
