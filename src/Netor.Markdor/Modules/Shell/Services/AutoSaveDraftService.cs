using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using Netor.Markdor.Core.Models;
using Netor.Markdor.Core.Services;

namespace Netor.Markdor.Modules.Shell.Services;

public sealed class AutoSaveDraftService : IAutoSaveDraftService
{
    private static readonly Encoding DraftEncoding = new UTF8Encoding(false);
    private readonly object _sync = new();
    private readonly string _session = Guid.NewGuid().ToString("N");
    private readonly Dictionary<string, string> _restoredPaths = new();
    private AutoSaveDraft? _pendingDraft;
    private Timer? _timer;
    public string StorageDirectory { get; init; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Markdor", "Drafts");
    public string? LastError { get; private set; }
    public event EventHandler? StateChanged;

    public DocumentSnapshot? TryRestore(DocumentSnapshot document)
    {
        if (!Directory.Exists(StorageDirectory))
        {
            WriteDiagnostic("restore skipped; directory missing", string.Empty, null, StorageDirectory);
            return null;
        }
        var key = GetDocumentKey(document);
        try
        {
            // 每个进程独立保存，打开文件或新建窗口不会覆盖尚未查看的恢复数据。
            foreach (var path in Directory.EnumerateFiles(StorageDirectory, key + ".*.json").OrderByDescending(File.GetLastWriteTimeUtc))
            {
                if (path == GetDraftPath(document)) continue;
                try
                {
                    WriteDiagnostic("restore reading", path, null);
                    var draft = JsonSerializer.Deserialize(File.ReadAllText(path, DraftEncoding), AutoSaveDraftJsonContext.Default.AutoSaveDraft);
                    if (draft?.Markdown is null) throw new InvalidDataException("Invalid recovery draft: " + path);
                    if (MarkdownTextComparer.EqualsNormalizedLineEndings(draft.Markdown, document.Markdown)) continue;
                    _restoredPaths[key] = path;
                    return document with { Markdown = draft.Markdown };
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
                {
                    WriteDiagnostic("restore read failed", path, exception);
                    SetError(exception.Message);
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            WriteDiagnostic("restore enumeration failed", StorageDirectory, exception);
            SetError(exception.Message);
        }
        return null;
    }

    public void QueueSave(DocumentSnapshot document, string markdown, string lastSavedMarkdown)
    {
        if (MarkdownTextComparer.EqualsNormalizedLineEndings(markdown, lastSavedMarkdown)) { Clear(document); return; }
        lock (_sync)
        {
            _pendingDraft = AutoSaveDraft.FromDocument(document, markdown, GetDraftPath(document));
            WriteDiagnostic("queued", _pendingDraft.Path, null, $"length={markdown.Length}");
            _timer ??= new Timer(_ => Flush(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _timer.Change(TimeSpan.FromSeconds(1.5), Timeout.InfiniteTimeSpan);
        }
    }

    public void Clear(DocumentSnapshot document)
    {
        lock (_sync)
        {
            var path = GetDraftPath(document);
            if (_pendingDraft?.Path == path) { _pendingDraft = null; _timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan); }
            // 删除不存在的草稿应是幂等操作；父目录尚未创建时不要把清理误报成保存错误。
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { SetError(exception.Message); }
        }
    }

    public void ClearRestored(DocumentSnapshot document)
    {
        var key = GetDocumentKey(document);
        if (!_restoredPaths.TryGetValue(key, out var path)) return;
        try { File.Delete(path); _restoredPaths.Remove(key); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { SetError(exception.Message); }
    }

    public bool Flush()
    {
        lock (_sync)
        {
            if (_pendingDraft is not { } draft) return true;
            _timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            WriteDiagnostic("flush started", draft.Path, null);
            try
            {
                var directory = Path.GetDirectoryName(draft.Path);
                if (string.IsNullOrWhiteSpace(directory))
                    throw new DirectoryNotFoundException("Draft directory is empty: " + draft.Path);

                EnsureDraftDirectory(directory, draft.Path);
                var json = JsonSerializer.Serialize(draft, AutoSaveDraftJsonContext.Default.AutoSaveDraft);
                var temporaryPath = draft.Path + ".tmp";
                WriteDraftFile(temporaryPath, draft.Path, directory, json);
                _pendingDraft = null;
                SetError(null);
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // 保留待写快照供重试；失败不能伪装成已落盘。
                WriteDiagnostic("flush failed", draft.Path, exception);
                SetError(exception.Message);
                return false;
            }
        }
    }

    private void SetError(string? error) { LastError = error; StateChanged?.Invoke(this, EventArgs.Empty); }

    private static void EnsureDraftDirectory(string directory, string draftPath)
    {
        WriteDiagnostic("create directory", draftPath, null, directory);
        Directory.CreateDirectory(directory);
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException("Draft directory was not created: " + directory);
    }

    private static void WriteDraftFile(string temporaryPath, string draftPath, string directory, string json)
    {
        WriteTemporaryFile(temporaryPath, draftPath, json);
        try
        {
            WriteDiagnostic("move temporary", draftPath, null, temporaryPath);
            File.Move(temporaryPath, draftPath, true);
        }
        catch (DirectoryNotFoundException)
        {
            // A cleanup tool can remove the directory between the write and move.
            WriteDiagnostic("directory missing during move; recreating", draftPath, null, directory);
            EnsureDraftDirectory(directory, draftPath);
            WriteTemporaryFile(temporaryPath, draftPath, json);
            File.Move(temporaryPath, draftPath, true);
        }
    }

    private static void WriteTemporaryFile(string temporaryPath, string draftPath, string json)
    {
        WriteDiagnostic("write temporary", draftPath, null, temporaryPath);
        File.WriteAllText(temporaryPath, json, DraftEncoding);
    }

    private static void WriteDiagnostic(string stage, string draftPath, Exception? exception, string? detail = null)
    {
        var line = $"{DateTimeOffset.UtcNow:O}\t{stage}\tdraft={draftPath}\tdetail={detail ?? string.Empty}\terror={exception?.ToString() ?? string.Empty}{Environment.NewLine}";
        try
        {
            var logPath = Path.Combine(Path.GetTempPath(), "Markdor-drafts.log");
            File.AppendAllText(logPath, line, Encoding.UTF8);
        }
        catch (Exception logException) when (logException is IOException or UnauthorizedAccessException)
        {
            Trace.WriteLine(line);
        }
    }

    private string GetDraftPath(DocumentSnapshot document) => Path.Combine(StorageDirectory, GetDocumentKey(document) + "." + _session + ".json");
    private static string GetDocumentKey(DocumentSnapshot document) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        document.FilePath is { Length: > 0 } path ? Path.GetFullPath(path) : "__untitled__"))).ToLowerInvariant();
}
