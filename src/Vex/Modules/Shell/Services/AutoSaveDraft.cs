using System.Text.Json.Serialization;
using Vex.Core.Models;

namespace Vex.Modules.Shell.Services;

internal sealed record AutoSaveDraft(
    string Path,
    string? FilePath,
    string FileName,
    string Markdown,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? SourceLastWriteTimeUtc)
{
    public static AutoSaveDraft FromDocument(DocumentSnapshot document, string markdown, string path)
    {
        DateTimeOffset? sourceWriteTime = null;
        if (document.FilePath is { Length: > 0 } filePath && File.Exists(filePath))
        {
            sourceWriteTime = new DateTimeOffset(File.GetLastWriteTimeUtc(filePath));
        }

        return new AutoSaveDraft(
            path,
            document.FilePath,
            document.FileName,
            markdown,
            DateTimeOffset.UtcNow,
            sourceWriteTime);
    }
}

[JsonSerializable(typeof(AutoSaveDraft))]
internal sealed partial class AutoSaveDraftJsonContext : JsonSerializerContext;
