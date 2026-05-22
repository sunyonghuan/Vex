using CodeWF.EventBus;
using ReactiveUI;
using Vex.Core.Messaging;
using Vex.Core.Models;
using Vex.Core.Services;

namespace Vex.Modules.Shell.ViewModels;

public sealed class MainWindowViewModel : ReactiveObject
{
    private readonly IDocumentService _documentService;
    private readonly IMarkdownExportService _exportService;
    private readonly IMarkdownOutlineService _outlineService;
    private readonly IMarkdownStatisticsService _statisticsService;
    private readonly IEventBus _eventBus;
    private DocumentSnapshot _document;
    private IReadOnlyList<DocumentFile> _documentFiles = [];
    private string _lastSavedMarkdown = string.Empty;
    private string _markdown = string.Empty;

    public MainWindowViewModel(
        IDocumentService documentService,
        IMarkdownExportService exportService,
        IMarkdownOutlineService outlineService,
        IMarkdownStatisticsService statisticsService,
        ShellAppearanceViewModel appearance,
        ShellDocumentInfoViewModel documentInfo,
        ShellDialogsViewModel dialogs,
        ShellEditorActionsViewModel editorActions,
        ShellEditorDisplayViewModel editorDisplay,
        ShellFindBarViewModel findBar,
        ShellHelpViewModel help,
        ShellWindowLayoutViewModel layout,
        ShellNavigationViewModel navigation,
        ShellRecentDocumentsViewModel recent,
        ShellStatusViewModel status,
        IEventBus eventBus)
    {
        _documentService = documentService;
        _exportService = exportService;
        _outlineService = outlineService;
        _statisticsService = statisticsService;
        Appearance = appearance;
        DocumentInfo = documentInfo;
        Dialogs = dialogs;
        EditorActions = editorActions;
        EditorDisplay = editorDisplay;
        FindBar = findBar;
        Help = help;
        Layout = layout;
        Navigation = navigation;
        Recent = recent;
        Status = status;
        _eventBus = eventBus;
        _eventBus.Subscribe(this);

        _document = _documentService.CreateNew();
        _lastSavedMarkdown = _document.Markdown;
        Markdown = _document.Markdown;
        RefreshDocumentInfo();
    }

    public event EventHandler? CloseWindowRequested;

    public ShellAppearanceViewModel Appearance { get; }

    public ShellDocumentInfoViewModel DocumentInfo { get; }

    public ShellDialogsViewModel Dialogs { get; }

    public ShellEditorActionsViewModel EditorActions { get; }

    public ShellEditorDisplayViewModel EditorDisplay { get; }

    public ShellFindBarViewModel FindBar { get; }

    public ShellHelpViewModel Help { get; }

    public ShellWindowLayoutViewModel Layout { get; }

    public ShellNavigationViewModel Navigation { get; }

    public ShellRecentDocumentsViewModel Recent { get; }

    public ShellStatusViewModel Status { get; }

    public async Task OpenStartupDocumentAsync(IEnumerable<string> arguments)
    {
        var path = arguments.FirstOrDefault(argument => File.Exists(argument) || Directory.Exists(argument));
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (Directory.Exists(path))
        {
            await ApplyDocumentFilesAsync(await _documentService.OpenFolderPathAsync(path), true);
            return;
        }

        ApplyDocument(await _documentService.OpenPathAsync(path));
    }

    public Task OpenDroppedPathAsync(string path)
    {
        if (Directory.Exists(path))
        {
            return RequestUnsavedConfirmationAsync(
                "Save changes before opening a folder?",
                $"Save changes to {_document.FileName} before opening the dropped folder?",
                () => OpenFolderPathCoreAsync(path));
        }

        if (!File.Exists(path))
        {
            SetStatus("Dropped item is unavailable.");
            return Task.CompletedTask;
        }

        if (!_documentService.IsSupportedDocumentPath(path))
        {
            SetStatus("Drop a Markdown or text file.");
            return Task.CompletedTask;
        }

        return RequestUnsavedConfirmationAsync(
            "Save changes before opening?",
            $"Save changes to {_document.FileName} before opening {Path.GetFileName(path)}?",
            () => OpenPathCoreAsync(path));
    }

    public string Markdown
    {
        get => _markdown;
        set
        {
            var normalized = value ?? string.Empty;
            if (_markdown != normalized)
            {
                this.RaiseAndSetIfChanged(ref _markdown, normalized);
                _document = _document with { Markdown = _markdown };
                RefreshMarkdownDerivedState();
            }
        }
    }

    public Task NewDocument()
    {
        return RequestUnsavedConfirmationAsync(
            "Save changes?",
            $"Save changes to {_document.FileName} before creating a new document?",
            () =>
            {
                NewDocumentCore();
                return Task.CompletedTask;
            });
    }

    private void NewDocumentCore()
    {
        _document = _documentService.CreateNew();
        _lastSavedMarkdown = _document.Markdown;
        Markdown = _document.Markdown;
        SetStatus("New document created.");
        RefreshDocumentInfo();
        EditorActions.FocusEditor();
    }

    public Task CloseDocument()
    {
        return RequestUnsavedConfirmationAsync(
            "Save changes?",
            $"Save changes to {_document.FileName} before closing this document?",
            () =>
            {
                CloseDocumentCore();
                return Task.CompletedTask;
            });
    }

    private void CloseDocumentCore()
    {
        _document = _documentService.CreateNew();
        _lastSavedMarkdown = _document.Markdown;
        Markdown = _document.Markdown;
        _documentFiles = [];
        _eventBus.Publish(new DocumentFilesChangedCommand(_documentFiles));
        SetStatus("Document closed.");
        RefreshDocumentInfo();
        EditorActions.FocusEditor();
    }

    public async Task OpenAsync()
    {
        await RequestUnsavedConfirmationAsync(
            "Save changes before opening?",
            $"Save changes to {_document.FileName} before opening another file?",
            OpenAsyncCore);
    }

    private async Task OpenAsyncCore()
    {
        var snapshot = await _documentService.OpenAsync();
        if (snapshot is not null)
        {
            ApplyDocument(snapshot);
        }
    }

    public async Task QuickOpenAsync()
    {
        if (_documentFiles.Count > 0)
        {
            SelectSidebarTab(0);
            SetStatus("Choose a document from the loaded folder.");
            return;
        }

        await OpenAsync();
    }

    public async Task OpenFolderAsync()
    {
        await RequestUnsavedConfirmationAsync(
            "Save changes before opening a folder?",
            $"Save changes to {_document.FileName} before opening a folder?",
            OpenFolderAsyncCore);
    }

    private async Task OpenFolderAsyncCore()
    {
        await ApplyDocumentFilesAsync(await _documentService.OpenFolderAsync(), true);
    }

    private async Task OpenPathCoreAsync(string path)
    {
        ApplyDocument(await _documentService.OpenPathAsync(path));
    }

    private async Task OpenFolderPathCoreAsync(string folder)
    {
        await ApplyDocumentFilesAsync(await _documentService.OpenFolderPathAsync(folder), true);
    }

    private async Task ApplyDocumentFilesAsync(IReadOnlyList<DocumentFile> files, bool bypassUnsavedPrompt = false)
    {
        _documentFiles = files.ToArray();
        var firstFile = _documentFiles.FirstOrDefault();
        _eventBus.Publish(new DocumentFilesChangedCommand(_documentFiles, firstFile));

        SetStatus(_documentFiles.Count == 0 ? "No markdown files loaded." : $"Loaded {_documentFiles.Count} markdown files.");
        if (firstFile is not null)
        {
            if (bypassUnsavedPrompt)
            {
                await OpenDocumentFileCoreAsync(firstFile);
            }
            else
            {
                await OpenDocumentFileAsync(firstFile, null);
            }
        }
    }

    private async Task OpenDocumentFileAsync(DocumentFile file, DocumentFile? previousSelection)
    {
        if (DocumentInfo.CurrentFilePath?.Equals(file.Path, StringComparison.OrdinalIgnoreCase) == true)
        {
            return;
        }

        await RequestUnsavedConfirmationAsync(
            "Save changes before switching files?",
            $"Save changes to {_document.FileName} before opening {file.Name}?",
            () => OpenDocumentFileCoreAsync(file),
            () => RestoreDocumentFileSelection(previousSelection));
    }

    private async Task OpenDocumentFileCoreAsync(DocumentFile file)
    {
        var snapshot = await _documentService.OpenPathAsync(file.Path);
        ApplyDocument(snapshot);
    }

    public async Task SaveAsync()
    {
        var saved = await _documentService.SaveAsync(_document with { Markdown = Markdown });
        if (saved is not null)
        {
            ApplyDocument(saved, false);
            SetStatus($"Saved {saved.FileName}.");
        }
    }

    public async Task SaveAsAsync()
    {
        var saved = await _documentService.SaveAsAsync(_document with { Markdown = Markdown });
        if (saved is not null)
        {
            ApplyDocument(saved, false);
            SetStatus($"Saved as {saved.FileName}.");
        }
    }

    public async Task SaveAllAsync()
    {
        // 当前仍是单文档编辑器，保留“保存全部”入口时必须明确只保存当前文档。
        await SaveAsync();
        SetStatus(DocumentInfo.IsModified
            ? "Save all canceled. Current document is still modified."
            : "Saved current document. Multi-document save is not available yet.");
    }

    public Task DeleteAsync()
    {
        if (DocumentInfo.CurrentFilePath is not { Length: > 0 } path)
        {
            return Task.CompletedTask;
        }

        return RequestUnsavedConfirmationAsync(
            "Save changes before deleting?",
            $"Save changes to {_document.FileName} before deleting it?",
            () =>
            {
                Dialogs.ShowDeleteConfirmation(path);
                return Task.CompletedTask;
            });
    }

    public async Task ConfirmDeleteAsync()
    {
        if (Dialogs.PendingDeletePath is not { Length: > 0 } path)
        {
            Dialogs.ClearDeleteConfirmation();
            return;
        }

        await _documentService.DeleteAsync(path);
        Recent.RemoveRecentDocument(path);
        Dialogs.ClearDeleteConfirmation();
        NewDocumentCore();
        SetStatus("File deleted.");
    }

    public async Task OpenFileLocationAsync()
    {
        if (DocumentInfo.CurrentFilePath is { Length: > 0 } path)
        {
            await _documentService.OpenFileLocationAsync(path);
        }
    }

    public async Task ReopenWithEncodingAsync(string? encodingName)
    {
        if (DocumentInfo.CurrentFilePath is not { Length: > 0 } path || string.IsNullOrWhiteSpace(encodingName))
        {
            SetStatus("Open a file before choosing an encoding.");
            return;
        }

        await RequestUnsavedConfirmationAsync(
            "Save changes before reopening?",
            $"Save changes to {_document.FileName} before reopening it with {encodingName}?",
            () => ReopenWithEncodingCoreAsync(path, encodingName));
    }

    private async Task ReopenWithEncodingCoreAsync(string path, string encodingName)
    {
        ApplyDocument(await _documentService.OpenPathAsync(path, encodingName));
        SetStatus($"Reopened with {encodingName}.");
    }

    private void ApplyDocument(DocumentSnapshot snapshot, bool updateMarkdown = true)
    {
        _document = snapshot;
        _lastSavedMarkdown = snapshot.Markdown;
        if (snapshot.FilePath is { Length: > 0 } path)
        {
            Recent.AddRecentDocument(path);
        }

        if (updateMarkdown)
        {
            Markdown = snapshot.Markdown;
        }
        else
        {
            RefreshDocumentInfo();
        }

        RefreshDocumentInfo();
        SetStatus($"Opened {snapshot.FileName}.");
        EditorActions.FocusEditor();
    }

    [EventHandler]
    public void ApplyMarkdownTextChanged(MarkdownTextChangedCommand command)
    {
        if (Markdown != command.Markdown)
        {
            Markdown = command.Markdown;
        }
    }

    [EventHandler]
    public void ApplyDocumentFileOpenRequested(DocumentFileOpenRequestedCommand command)
    {
        _ = OpenDocumentFileAsync(command.File, command.PreviousSelection);
    }

    private void RefreshMarkdownDerivedState()
    {
        RefreshDocumentInfo();
        _eventBus.Publish(new OutlineItemsChangedCommand(_outlineService.BuildOutline(Markdown)));
    }

    private void RefreshDocumentInfo()
    {
        DocumentInfo.Refresh(_document, Markdown, _lastSavedMarkdown, _statisticsService.Count(Markdown));
    }

    public void ShowProperties()
    {
        Dialogs.ShowPropertiesPanel();
        SetStatus($"{DocumentInfo.CurrentDocumentTitle} | {DocumentInfo.DocumentStateText} | {DocumentInfo.CurrentEncodingText} | {DocumentInfo.PropertySizeText} | {DocumentInfo.PropertyLocationText}");
    }

    public async Task Export(string? format)
    {
        if (format?.Equals("HTML", StringComparison.OrdinalIgnoreCase) == true)
        {
            var path = await _exportService.ExportHtmlAsync(_document with { Markdown = Markdown });
            SetStatus(path is null ? "HTML export canceled." : $"Exported HTML to {Path.GetFileName(path)}.");
            return;
        }

        SetStatus($"{format?.ToUpperInvariant() ?? "Document"} export is not implemented yet.");
    }

    public async Task Print()
    {
        var path = await _exportService.OpenHtmlPrintPreviewAsync(_document with { Markdown = Markdown });
        SetStatus(path is null ? "Print preview canceled." : "Opened HTML print preview.");
    }

    public void WordCount()
    {
        Dialogs.ShowStatisticsPanel();
        SetStatus($"Words {DocumentInfo.Statistics.Words}, Characters {DocumentInfo.Statistics.Characters}, Lines {DocumentInfo.Statistics.Lines}, Reading {DocumentInfo.Statistics.ReadingMinutes} min.");
    }

    public bool CloseFloatingPanel() => Dialogs.CloseFloatingPanel();

    public void ShowFindPanel() => FindBar.ShowFindPanel();

    public void ShowReplacePanel() => FindBar.ShowReplacePanel();

    public void CloseFindPanel() => FindBar.CloseFindPanel();

    public void FindNext() => FindBar.FindNext();

    public void ReplaceNext() => FindBar.ReplaceNext();

    public void ReplaceAll() => FindBar.ReplaceAll();

    private void SetStatus(string message)
    {
        _eventBus.Publish(new WorkspaceStatusChangedCommand(message));
    }

    private void SelectSidebarTab(int selectedIndex)
    {
        _eventBus.Publish(new ShellSidebarTabSelectedCommand(selectedIndex));
    }

    private void RestoreDocumentFileSelection(DocumentFile? previousSelection)
    {
        _eventBus.Publish(new DocumentFileSelectionChangedCommand(previousSelection));
    }

    public async Task OpenRecentDocumentAsync(int index)
    {
        if (!Recent.TryGetDocument(index, out var recent) || recent is null)
        {
            SetStatus("Recent file is unavailable.");
            return;
        }

        if (!File.Exists(recent.Path))
        {
            Recent.RemoveRecentDocument(recent.Path);
            SetStatus("Recent file was removed because it no longer exists.");
            return;
        }

        await RequestUnsavedConfirmationAsync(
            "Save changes before opening recent file?",
            $"Save changes to {_document.FileName} before opening {recent.DisplayText}?",
            async () => ApplyDocument(await _documentService.OpenPathAsync(recent.Path)));
    }

    public Task BeginWindowCloseAsync()
    {
        return RequestUnsavedConfirmationAsync(
            "Save changes before closing Vex?",
            $"Save changes to {_document.FileName} before closing Vex?",
            () =>
            {
                CloseWindowRequested?.Invoke(this, EventArgs.Empty);
                return Task.CompletedTask;
            });
    }

    public async Task SavePendingActionAsync()
    {
        if (!Dialogs.HasPendingUnsavedAction)
        {
            return;
        }

        await SaveAsync();
        if (DocumentInfo.IsModified)
        {
            SetStatus("Save canceled. Action was not completed.");
            return;
        }

        await ContinuePendingActionAsync();
    }

    public Task DiscardPendingActionAsync()
    {
        return ContinuePendingActionAsync();
    }

    private async Task RequestUnsavedConfirmationAsync(
        string title,
        string message,
        Func<Task> continuation,
        Action? cancellation = null)
    {
        if (!DocumentInfo.IsModified)
        {
            await continuation();
            return;
        }

        Dialogs.ShowUnsavedConfirmation(
            title,
            message,
            DocumentInfo.CurrentFilePath ?? "Unsaved document",
            continuation,
            cancellation);
    }

    private async Task ContinuePendingActionAsync()
    {
        var continuation = Dialogs.TakePendingUnsavedContinuation();
        if (continuation is not null)
        {
            await continuation();
        }
    }

}
