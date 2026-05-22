using Vex.Core.Models;
using Vex.Core.Services;

namespace Vex.Modules.Workspace.Services;

public sealed class DocumentFileFactory : IDocumentFileFactory
{
    private readonly IAppLocalizer _localizer;

    public DocumentFileFactory(IAppLocalizer localizer)
    {
        _localizer = localizer;
    }

    public DocumentFile Create(string path, string? workspaceRoot = null)
    {
        // 文件列表展示文案集中在工厂内，避免 DocumentFile 反向依赖运行时本地化服务。
        return new DocumentFile(
            path,
            Path.GetFileName(path),
            ResolveFolderName(path, workspaceRoot),
            ResolveModifiedText(path),
            ResolvePreview(path));
    }

    private string ResolveModifiedText(string path)
    {
        if (!File.Exists(path))
        {
            return string.Empty;
        }

        DateTimeOffset lastWriteTime;
        try
        {
            lastWriteTime = new DateTimeOffset(File.GetLastWriteTime(path));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return string.Empty;
        }

        var age = DateTimeOffset.Now - lastWriteTime;
        return age.TotalDays switch
        {
            < 1 => lastWriteTime.ToString("HH:mm", _localizer.Culture),
            < 2 => _localizer.Get(VexL.DocumentModifiedYesterday),
            < 8 => _localizer.Format(VexL.DocumentModifiedDaysAgoFormat, (int)age.TotalDays),
            _ => lastWriteTime.ToString("MMM dd", _localizer.Culture)
        };
    }

    private static string ResolveFolderName(string path, string? workspaceRoot)
    {
        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return "Vex";
        }

        if (!string.IsNullOrWhiteSpace(workspaceRoot))
        {
            var relative = Path.GetRelativePath(workspaceRoot, directory);
            if (!relative.StartsWith("..", StringComparison.Ordinal))
            {
                return relative == "." ? Path.GetFileName(workspaceRoot) : relative;
            }
        }

        return Path.GetFileName(directory);
    }

    private static string ResolvePreview(string path)
    {
        if (!File.Exists(path))
        {
            return string.Empty;
        }

        try
        {
            foreach (var line in File.ReadLines(path).Take(8))
            {
                var trimmed = line.Trim().TrimStart('#', '-', '*', '>', ' ');
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    return trimmed.Length > 96 ? $"{trimmed[..96]}..." : trimmed;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return string.Empty;
        }

        return string.Empty;
    }
}
