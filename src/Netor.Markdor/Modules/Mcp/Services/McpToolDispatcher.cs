using System.Reflection;
using System.Text;
using System.Text.Json;
using Avalonia.Threading;
using CodeWF.Toolkit.EventBus;
using Markdig;
using Netor.Markdor.Core.Messaging;
using Netor.Markdor.Core.Services;
using Netor.Markdor.Modules.Mcp.Models;
using Netor.Markdor.Modules.Mcp.Serialization;
using Netor.Markdor.Modules.Shell.ViewModels;

namespace Netor.Markdor.Modules.Mcp.Services;

public sealed class McpToolDispatcher : IMcpToolDispatcher
{
    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    private static readonly StringComparer PathComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;
    private static readonly StringComparison PathComparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;
    private const string EmptySchema = """{"type":"object","properties":{},"additionalProperties":false}""";
    private const string ReplaceDocumentSchema = """{"type":"object","properties":{"markdown":{"type":"string"},"reason":{"type":"string"}},"required":["markdown"],"additionalProperties":false}""";
    private const string ApplyTextEditSchema = """{"type":"object","properties":{"startOffset":{"type":"integer","minimum":0},"length":{"type":"integer","minimum":0},"replacement":{"type":"string"},"reason":{"type":"string"}},"required":["startOffset","length","replacement"],"additionalProperties":false}""";
    private const string InsertTextSchema = """{"type":"object","properties":{"offset":{"type":"integer","minimum":0},"text":{"type":"string"}},"required":["text"],"additionalProperties":false}""";
    private const string ReplaceSelectionSchema = """{"type":"object","properties":{"text":{"type":"string"},"reason":{"type":"string"}},"required":["text"],"additionalProperties":false}""";
    private const string OpenDocumentSchema = """{"type":"object","properties":{"path":{"type":"string"},"encodingName":{"type":"string"}},"required":["path"],"additionalProperties":false}""";
    private const string UiSetThemeSchema = """{"type":"object","properties":{"themeKey":{"type":"string","enum":["system","light","dark"]}},"required":["themeKey"],"additionalProperties":false}""";
    private const string UiSetTypographySchema = """{"type":"object","properties":{"typographyKey":{"type":"string"}},"required":["typographyKey"],"additionalProperties":false}""";
    private const string UiSetLanguageSchema = """{"type":"object","properties":{"cultureName":{"type":"string","enum":["zh-CN","zh-Hant","en-US","ja-JP"]}},"required":["cultureName"],"additionalProperties":false}""";
    private const string UiSetLayoutSchema = """{"type":"object","properties":{"sidebarVisible":{"type":"boolean"},"statusBarVisible":{"type":"boolean"},"previewVisible":{"type":"boolean"},"sourceMode":{"type":"boolean"},"lineNumbersVisible":{"type":"boolean"},"compactLayout":{"type":"boolean"}},"additionalProperties":false}""";
    private const string UiShowSidebarTabSchema = """{"type":"object","properties":{"tab":{"type":"string","enum":["files","outline"]}},"required":["tab"],"additionalProperties":false}""";
    private const string UiOpenPanelSchema = """{"type":"object","properties":{"panel":{"type":"string","enum":["find","replace","properties","wordCount","mcpSettings","changelog","thanks","about"]}},"required":["panel"],"additionalProperties":false}""";
    private const string UiApplyEditorCommandSchema = """{"type":"object","properties":{"command":{"type":"string","enum":["undo","redo","copy","selectAll","paragraph","heading1","heading2","heading3","bold","italic","inlineCode","link","image","quote","orderedList","unorderedList","taskList","codeFence","mathBlock","table","horizontalRule","clearFormatting"]}},"required":["command"],"additionalProperties":false}""";
    private const string ExportCurrentDocumentSchema = """{"type":"object","properties":{"format":{"type":"string","enum":["HTML","PDF","PNG","Word"]}},"required":["format"],"additionalProperties":false}""";
    private const string CopyRenderedHtmlSchema = """{"type":"object","properties":{"target":{"type":"string","enum":["wechat","zhihu","juejin"]}},"required":["target"],"additionalProperties":false}""";
    private const string ReplaceTextSchema = """{"type":"object","properties":{"find":{"type":"string","minLength":1},"replacement":{"type":"string"},"replace_all":{"type":"boolean"}},"required":["find","replacement"],"additionalProperties":false}""";
    private const string CreateDocumentSchema = """{"type":"object","properties":{"path":{"type":"string"},"content":{"type":"string"},"overwrite":{"type":"boolean"},"reason":{"type":"string"}},"required":["path","content"],"additionalProperties":false}""";

    private readonly MainWindowViewModel _shell;
    private readonly IMarkdownOutlineService _outlineService;
    private readonly IDocumentService _documentService;
    private readonly IAppSettingsStore _settingsStore;
    private readonly IMcpOperationConfirmationService _confirmationService;
    private readonly IMcpOperationAuditService _auditService;
    private readonly IAppLocalizer _localizer;
    private readonly List<McpEditSnapshot> _undoStack = [];
    private readonly List<McpEditSnapshot> _redoStack = [];

    private sealed record McpEditSnapshot(string? FilePath, string Markdown);

    public McpToolDispatcher(
        MainWindowViewModel shell,
        IMarkdownOutlineService outlineService,
        IDocumentService documentService,
        IAppSettingsStore settingsStore,
        IMcpOperationConfirmationService confirmationService,
        IMcpOperationAuditService auditService,
        IAppLocalizer localizer)
    {
        _shell = shell;
        _outlineService = outlineService;
        _documentService = documentService;
        _settingsStore = settingsStore;
        _confirmationService = confirmationService;
        _auditService = auditService;
        _localizer = localizer;
    }

    public McpToolsListResult ListTools() => new(BuildTools());

    private McpToolDescription[] BuildTools()
    {
        return
        [
            Tool("markdor_get_current_document", _localizer.Get(MarkdorL.McpToolGetDocument)),
            Tool("markdor_get_document_outline", _localizer.Get(MarkdorL.McpToolGetOutline)),
            Tool("markdor_get_selection", _localizer.Get(MarkdorL.McpToolGetSelection)),
            Tool("markdor_replace_current_document", _localizer.Get(MarkdorL.McpToolReplaceDocument), ReplaceDocumentSchema),
            Tool("markdor_apply_text_edit", _localizer.Get(MarkdorL.McpToolApplyTextEdit), ApplyTextEditSchema),
            Tool("markdor_insert_text", _localizer.Get(MarkdorL.McpToolInsertText), InsertTextSchema),
            Tool("markdor_replace_selection", _localizer.Get(MarkdorL.McpToolReplaceSelection), ReplaceSelectionSchema),
            Tool("markdor_replace_text", _localizer.Get(MarkdorL.McpToolReplaceText), ReplaceTextSchema),
            Tool("markdor_open_document", _localizer.Get(MarkdorL.McpToolOpenDocument), OpenDocumentSchema),
            Tool("markdor_save_current_document", _localizer.Get(MarkdorL.McpToolSaveDocument)),
            Tool("markdor_undo", _localizer.Get(MarkdorL.McpToolUndo)),
            Tool("markdor_redo", _localizer.Get(MarkdorL.McpToolRedo)),
            Tool("markdor_list_files", _localizer.Get(MarkdorL.McpToolListFiles)),
            Tool("markdor_create_document", _localizer.Get(MarkdorL.McpToolCreateDocument), CreateDocumentSchema),
            Tool("markdor_refresh_preview", _localizer.Get(MarkdorL.McpToolRefreshPreview)),
            Tool("markdor_get_rendered_html", _localizer.Get(MarkdorL.McpToolGetRenderedHtml)),
            Tool("markdor_get_app_status", _localizer.Get(MarkdorL.McpToolGetAppStatus)),
            Tool("markdor_get_operation_audit", _localizer.Get(MarkdorL.McpToolGetAudit)),
            Tool("markdor_ui_get_state", _localizer.Get(MarkdorL.McpToolUiGetState)),
            Tool("markdor_ui_set_theme", _localizer.Get(MarkdorL.McpToolUiSetTheme), UiSetThemeSchema),
            Tool("markdor_ui_set_typography", _localizer.Get(MarkdorL.McpToolUiSetTypography), UiSetTypographySchema),
            Tool("markdor_ui_set_language", _localizer.Get(MarkdorL.McpToolUiSetLanguage), UiSetLanguageSchema),
            Tool("markdor_ui_set_layout", _localizer.Get(MarkdorL.McpToolUiSetLayout), UiSetLayoutSchema),
            Tool("markdor_ui_show_sidebar_tab", _localizer.Get(MarkdorL.McpToolUiShowSidebarTab), UiShowSidebarTabSchema),
            Tool("markdor_ui_open_panel", _localizer.Get(MarkdorL.McpToolUiOpenPanel), UiOpenPanelSchema),
            Tool("markdor_ui_refresh_preview", _localizer.Get(MarkdorL.McpToolUiRefreshPreview)),
            Tool("markdor_ui_apply_editor_command", _localizer.Get(MarkdorL.McpToolUiApplyEditorCommand), UiApplyEditorCommandSchema),
            Tool("markdor_ui_export_current_document", _localizer.Get(MarkdorL.McpToolUiExport), ExportCurrentDocumentSchema),
            Tool("markdor_ui_copy_rendered_html", _localizer.Get(MarkdorL.McpToolUiCopyHtml), CopyRenderedHtmlSchema)
        ];
    }

    public async Task<McpToolCallResult> CallToolAsync(string name, JsonElement? arguments)
    {
        var toolName = NormalizeToolName(name);
        try
        {
            var result = await Dispatcher.UIThread.InvokeAsync(async () => await CallToolOnUiThreadAsync(toolName, arguments));
            RecordAudit(toolName, result, error: null);
            return TextResult(SerializeToolResult(result));
        }
        catch (Exception exception)
        {
            RecordAudit(toolName, result: null, exception.Message);
            return TextResult(exception.Message, isError: true);
        }
    }

    private async Task<object> CallToolOnUiThreadAsync(string name, JsonElement? arguments)
    {
        switch (name)
        {
            case "markdor_get_current_document":
                return GetCurrentDocument();
            case "markdor_get_document_outline":
                return GetOutline();
            case "markdor_get_selection":
                return GetSelection();
            case "markdor_replace_current_document":
                return await ReplaceCurrentDocumentAsync(Read(arguments, McpJsonContext.Default.ReplaceCurrentDocumentInput));
            case "markdor_apply_text_edit":
                return await ApplyTextEditAsync(Read(arguments, McpJsonContext.Default.ApplyTextEditInput));
            case "markdor_insert_text":
                return await InsertTextAsync(Read(arguments, McpJsonContext.Default.InsertTextInput));
            case "markdor_replace_selection":
                return await ReplaceSelectionAsync(Read(arguments, McpJsonContext.Default.ReplaceSelectionInput));
            case "markdor_replace_text":
                return await ReplaceTextAsync(Read(arguments, McpJsonContext.Default.ReplaceTextInput));
            case "markdor_undo":
                return await UndoRedoAsync(undo: true, "markdor_undo");
            case "markdor_redo":
                return await UndoRedoAsync(undo: false, "markdor_redo");
            case "markdor_list_files":
                return await ListFilesAsync();
            case "markdor_create_document":
                return await CreateDocumentAsync(Read(arguments, McpJsonContext.Default.CreateDocumentInput));
            case "markdor_open_document":
                return await OpenDocumentAsync(Read(arguments, McpJsonContext.Default.OpenDocumentInput));
            case "markdor_save_current_document":
                return await SaveCurrentDocumentAsync();
            case "markdor_refresh_preview":
            case "markdor_ui_refresh_preview":
                _shell.RefreshPreview();
                return new OperationResult("ok", "preview refreshed");
            case "markdor_get_rendered_html":
                return new RenderedHtmlResult(Markdig.Markdown.ToHtml(_shell.Markdown, MarkdownPipeline));
            case "markdor_get_app_status":
                return GetAppStatus();
            case "markdor_get_operation_audit":
                return GetOperationAudit();
            case "markdor_ui_get_state":
                return GetUiState();
            case "markdor_ui_set_theme":
                return SetTheme(Read(arguments, McpJsonContext.Default.UiSetThemeInput));
            case "markdor_ui_set_typography":
                return SetTypography(Read(arguments, McpJsonContext.Default.UiSetTypographyInput));
            case "markdor_ui_set_language":
                return SetLanguage(Read(arguments, McpJsonContext.Default.UiSetLanguageInput));
            case "markdor_ui_set_layout":
                return SetLayout(Read(arguments, McpJsonContext.Default.UiSetLayoutInput));
            case "markdor_ui_show_sidebar_tab":
                return ShowSidebarTab(Read(arguments, McpJsonContext.Default.UiShowSidebarTabInput));
            case "markdor_ui_open_panel":
                return await OpenPanelAsync(Read(arguments, McpJsonContext.Default.UiOpenPanelInput));
            case "markdor_ui_apply_editor_command":
                return ApplyEditorCommand(Read(arguments, McpJsonContext.Default.UiApplyEditorCommandInput));
            case "markdor_ui_export_current_document":
                return await ExportCurrentDocumentAsync(Read(arguments, McpJsonContext.Default.ExportCurrentDocumentInput));
            case "markdor_ui_copy_rendered_html":
                return await CopyRenderedHtmlAsync(Read(arguments, McpJsonContext.Default.CopyRenderedHtmlInput));
            default:
                throw new InvalidOperationException($"Unknown tool: {name}");
        }
    }

    private CurrentDocumentResult GetCurrentDocument()
    {
        var document = _shell.GetCurrentDocumentSnapshot();
        return new CurrentDocumentResult(
            document.FilePath,
            document.FileName,
            _shell.Markdown,
            _shell.DocumentInfo.IsModified,
            GetEncodingDisplayName(document.Encoding),
            _shell.DocumentVersion);
    }

    private OutlineResult GetOutline()
    {
        return new OutlineResult(_outlineService.BuildOutline(_shell.Markdown)
            .Select(item => new OutlineItemResult(item.Level, item.Title, item.Line))
            .ToArray());
    }

    private static SelectionResult GetSelection()
    {
        var selection = CodeWF.Toolkit.EventBus.EventBus.Default.Query(new EditorSelectionQuery());
        return new SelectionResult(selection.Text, selection.StartOffset, selection.Length);
    }

    private async Task<OperationResult> ReplaceCurrentDocumentAsync(ReplaceCurrentDocumentInput input)
    {
        if (!await ConfirmIfRequiredAsync(
                "markdor_replace_current_document",
                GetCurrentTargetName(),
                input.Reason ?? "replace current markdown"))
        {
            return new OperationResult("canceled", "operation rejected");
        }

        PushUndoSnapshot();
        _shell.ReplaceMarkdownFromMcp(input.Markdown);
        return new OperationResult("ok", "document replaced");
    }

    private async Task<OperationResult> ApplyTextEditAsync(ApplyTextEditInput input)
    {
        if (!await ConfirmIfRequiredAsync(
                "markdor_apply_text_edit",
                GetCurrentTargetName(),
                input.Reason ?? $"offset {input.StartOffset}, length {input.Length}"))
        {
            return new OperationResult("canceled", "operation rejected");
        }

        PushUndoSnapshot();
        _shell.ApplyTextEditFromMcp(input.StartOffset, input.Length, input.Replacement);
        return new OperationResult("ok", "text edit applied");
    }

    private async Task<OperationResult> InsertTextAsync(InsertTextInput input)
    {
        if (!await ConfirmIfRequiredAsync(
                "markdor_insert_text",
                GetCurrentTargetName(),
                $"insert {input.Text.Length} characters"))
        {
            return new OperationResult("canceled", "operation rejected");
        }

        PushUndoSnapshot();
        var offset = input.Offset ?? _shell.Markdown.Length;
        _shell.ApplyTextEditFromMcp(offset, 0, input.Text);
        return new OperationResult("ok", "text inserted");
    }

    private async Task<OperationResult> ReplaceSelectionAsync(ReplaceSelectionInput input)
    {
        var selection = CodeWF.Toolkit.EventBus.EventBus.Default.Query(new EditorSelectionQuery());
        if (selection.Length <= 0)
        {
            throw new InvalidOperationException("No active editor selection.");
        }

        if (!await ConfirmIfRequiredAsync(
                "markdor_replace_selection",
                GetCurrentTargetName(),
                input.Reason ?? $"replace selection at offset {selection.StartOffset}"))
        {
            return new OperationResult("canceled", "operation rejected");
        }

        PushUndoSnapshot();
        _shell.ApplyTextEditFromMcp(selection.StartOffset, selection.Length, input.Text);
        return new OperationResult("ok", "selection replaced");
    }

    private async Task<OperationResult> ReplaceTextAsync(ReplaceTextInput input)
    {
        if (string.IsNullOrEmpty(input.Find))
        {
            throw new InvalidOperationException("find must not be empty.");
        }

        var markdown = _shell.Markdown;
        var occurrences = CountOccurrences(markdown, input.Find);
        if (occurrences == 0)
        {
            throw new InvalidOperationException("target text not found.");
        }

        if (occurrences > 1 && !input.ReplaceAll)
        {
            throw new InvalidOperationException(
                $"target text appears {occurrences} times; use a unique string or set replace_all.");
        }

        if (!await ConfirmIfRequiredAsync(
                "markdor_replace_text",
                GetCurrentTargetName(),
                (input.ReplaceAll ? $"replace all {occurrences} occurrence(s) of " : "replace occurrence of ")
                + Summarize(input.Find)))
        {
            return new OperationResult("canceled", "operation rejected");
        }

        PushUndoSnapshot();
        if (input.ReplaceAll)
        {
            _shell.ReplaceMarkdownFromMcp(markdown.Replace(input.Find, input.Replacement));
            return new OperationResult("ok", $"replaced {occurrences} occurrence(s)");
        }

        var index = markdown.IndexOf(input.Find, StringComparison.Ordinal);
        _shell.ApplyTextEditFromMcp(index, input.Find.Length, input.Replacement);
        return new OperationResult("ok", "replaced");
    }

    private async Task<OperationResult> UndoRedoAsync(bool undo, string toolName)
    {
        if (!await ConfirmIfRequiredAsync(
                toolName,
                GetCurrentTargetName(),
                undo ? "undo last MCP-applied change" : "redo last undone change"))
        {
            return new OperationResult("canceled", "operation rejected");
        }

        var currentPath = NormalizePathSeparators(GetCurrentTargetName());
        var source = undo ? _undoStack : _redoStack;
        if (source.Count == 0)
        {
            return new OperationResult("ok", undo ? "nothing to undo" : "nothing to redo");
        }

        var snapshot = source[^1];
        if (!PathComparer.Equals(snapshot.FilePath, currentPath))
        {
            return new OperationResult("canceled", "the recorded change belongs to a different document");
        }

        source.RemoveAt(source.Count - 1);
        var current = _shell.GetCurrentDocumentSnapshot();
        var redoOrUndo = undo ? _redoStack : _undoStack;
        redoOrUndo.Add(new McpEditSnapshot(currentPath, current.Markdown));
        _shell.ReplaceMarkdownFromMcp(snapshot.Markdown);
        return new OperationResult("ok", undo ? "undone" : "redone");
    }

    public async Task<ResourceListResult> ListResourcesAsync()
    {
        var files = await _shell.GetWorkspaceFilesAsync();
        return await Dispatcher.UIThread.InvokeAsync(() => BuildResources(files));
    }

    private ResourceListResult BuildResources(IReadOnlyList<Markdor.Core.Models.DocumentFile> files)
    {
        var document = _shell.GetCurrentDocumentSnapshot();
        var resources = new List<ResourceEntryResult>
        {
            new("markdor://current-document", document.FileName, "text/markdown", null)
        };

        foreach (var file in files)
        {
            resources.Add(new ResourceEntryResult(ResourceUriForPath(file.Path), file.Name, "text/markdown", null));
        }

        return new ResourceListResult(resources);
    }

    public ResourceReadResult ReadResource(string uri)
    {
        if (string.Equals(uri, "markdor://current-document", StringComparison.Ordinal))
        {
            return new ResourceReadResult(uri, "text/markdown", _shell.Markdown);
        }

        const string filePrefix = "markdor://file/";
        if (!uri.StartsWith(filePrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unknown resource uri: {uri}");
        }

        var path = ResolveAuthorizedDocumentPath(uri[filePrefix.Length..]);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Resource not found.", path);
        }

        return new ResourceReadResult(uri, "text/markdown", File.ReadAllText(path));
    }

    private static string ResourceUriForPath(string path)
    {
        return "markdor://file/" + path;
    }

    private async Task<ListFilesResult> ListFilesAsync()
    {
        var workspaceFiles = await _shell.GetWorkspaceFilesAsync();
        var current = NormalizePathSeparators(GetCurrentTargetName());
        var files = workspaceFiles
            .Select(file => new FileEntryResult(
                file.Path,
                file.Name,
                PathComparer.Equals(NormalizePathSeparators(file.Path), current)))
            .ToArray();
        return new ListFilesResult(_shell.WorkspaceFolderPath, files);
    }

    private static string? NormalizePathSeparators(string? path)
    {
        return path?.Replace('/', Path.DirectorySeparatorChar);
    }

    private async Task<OperationResult> CreateDocumentAsync(CreateDocumentInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Path))
        {
            throw new InvalidOperationException("Document path is required.");
        }

        if (!_documentService.IsSupportedDocumentPath(input.Path))
        {
            throw new InvalidOperationException("Unsupported document path.");
        }

        var fullPath = ResolveAuthorizedDocumentPath(input.Path);
        if (File.Exists(fullPath) && !input.Overwrite)
        {
            throw new InvalidOperationException("File already exists; set overwrite to replace it.");
        }

        if (!await ConfirmIfRequiredAsync(
                "markdor_create_document",
                fullPath,
                $"create document ({input.Content.Length} characters)"))
        {
            return new OperationResult("canceled", "operation rejected");
        }

        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(fullPath, input.Content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        var opened = await _shell.OpenPathFromMcpAsync(fullPath);
        return opened
            ? new OperationResult("ok", "document created and opened")
            : new OperationResult("canceled", "document created; opening was rejected");
    }

    private void PushUndoSnapshot()
    {
        var document = _shell.GetCurrentDocumentSnapshot();
        _undoStack.Add(new McpEditSnapshot(
            NormalizePathSeparators(document.FilePath ?? document.FileName),
            document.Markdown));
        _redoStack.Clear();
        if (_undoStack.Count > 50)
        {
            _undoStack.RemoveAt(0);
        }
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static string Summarize(string text)
    {
        var clipped = text.Length <= 40 ? text : text[..40] + "…";
        return '"' + clipped + '"';
    }

    private async Task<OperationResult> SaveCurrentDocumentAsync()
    {
        if (!await ConfirmIfRequiredAsync(
                "markdor_save_current_document",
                GetCurrentTargetName(),
                "save current document"))
        {
            return new OperationResult("canceled", "operation rejected");
        }

        await _shell.SaveAsync();
        return new OperationResult("ok", "saved");
    }

    private async Task<OperationResult> OpenDocumentAsync(OpenDocumentInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Path))
        {
            throw new InvalidOperationException("Document path is required.");
        }

        if (!_documentService.IsSupportedDocumentPath(input.Path))
        {
            throw new InvalidOperationException("Unsupported document path.");
        }

        var fullPath = ResolveAuthorizedDocumentPath(input.Path);
        if (!await ConfirmIfRequiredAsync("markdor_open_document", fullPath, "open document"))
        {
            return new OperationResult("canceled", "operation rejected");
        }

        if (!await _shell.OpenPathFromMcpAsync(fullPath, input.EncodingName))
        {
            return new OperationResult("canceled", "operation rejected");
        }

        return new OperationResult("ok", "document opened");
    }

    private string ResolveAuthorizedDocumentPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (fullPath.StartsWith(@"\\", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("UNC paths are not allowed.");
        }

        var settings = _settingsStore.Current;
        var scope = settings.McpAccessScope;
        if (scope == McpAccessScope.CurrentFolder)
        {
            var currentFolder = _shell.DocumentInfo.CurrentFilePath is { Length: > 0 } currentPath
                ? Path.GetDirectoryName(currentPath)
                : null;
            EnsureUnderRoot(fullPath, currentFolder, "Current document folder is unavailable.");
            return fullPath;
        }

        if (scope == McpAccessScope.CustomFolder)
        {
            EnsureUnderRoot(fullPath, settings.McpAllowedWorkspacePath, "Custom MCP folder is unavailable.");
            return fullPath;
        }

        var currentDocumentPath = _shell.DocumentInfo.CurrentFilePath;
        if (string.IsNullOrWhiteSpace(currentDocumentPath) || !PathComparer.Equals(fullPath, Path.GetFullPath(currentDocumentPath)))
        {
            throw new InvalidOperationException("Path is outside the current document scope.");
        }

        return fullPath;
    }

    private static void EnsureUnderRoot(string fullPath, string? rootPath, string missingRootMessage)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new InvalidOperationException(missingRootMessage);
        }

        var root = Path.GetFullPath(rootPath);
        if (!Directory.Exists(root))
        {
            throw new InvalidOperationException("Authorized folder does not exist.");
        }

        var normalizedRoot = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;
        if (!PathComparer.Equals(fullPath, root) && !fullPath.StartsWith(normalizedRoot, PathComparison))
        {
            throw new InvalidOperationException("Path is outside the authorized folder.");
        }
    }

    private Task<bool> ConfirmIfRequiredAsync(string toolName, string target, string summary)
    {
        return _settingsStore.Current.McpRequireConfirmation == false
            ? Task.FromResult(true)
            : _confirmationService.ConfirmAsync(toolName, target, summary);
    }

    private void RecordAudit(string toolName, object? result, string? error)
    {
        var operationResult = result as OperationResult;
        var succeeded = error is null && operationResult?.Status != "canceled";
        var confirmed = error is null && operationResult?.Status != "canceled";
        var requiresConfirmation = ToolRequiresConfirmation(toolName);
        _auditService.Record(new McpOperationRecord(
            DateTimeOffset.Now,
            toolName,
            ResolveAuditTarget(toolName),
            ResolveOperationType(toolName),
            requiresConfirmation,
            !requiresConfirmation || confirmed,
            succeeded,
            error ?? (operationResult?.Status == "canceled" ? operationResult.Detail : null)));
    }

    private string ResolveAuditTarget(string toolName)
    {
        return toolName switch
        {
            "markdor_ui_copy_rendered_html" => "clipboard",
            "markdor_ui_set_theme" or "markdor_ui_set_typography" or "markdor_ui_set_language" or "markdor_ui_set_layout"
                or "markdor_ui_show_sidebar_tab" or "markdor_ui_open_panel" or "markdor_ui_refresh_preview"
                or "markdor_ui_apply_editor_command" => "ui",
            _ => GetCurrentTargetName()
        };
    }

    private static string ResolveOperationType(string toolName)
    {
        return toolName switch
        {
            "markdor_get_current_document" or "markdor_get_document_outline" or "markdor_get_selection"
                or "markdor_get_rendered_html" or "markdor_get_app_status" or "markdor_ui_get_state"
                or "markdor_list_files" => "read",
            "markdor_replace_current_document" or "markdor_apply_text_edit" or "markdor_insert_text"
                or "markdor_replace_selection" => "edit",
            "markdor_open_document" => "open",
            "markdor_create_document" => "create",
            "markdor_save_current_document" => "save",
            "markdor_ui_export_current_document" => "export",
            "markdor_ui_copy_rendered_html" => "clipboard",
            _ => "ui"
        };
    }

    private static bool ToolRequiresConfirmation(string toolName)
    {
        return toolName is "markdor_replace_current_document"
            or "markdor_apply_text_edit"
            or "markdor_insert_text"
            or "markdor_replace_selection"
            or "markdor_replace_text"
            or "markdor_undo"
            or "markdor_redo"
            or "markdor_create_document"
            or "markdor_open_document"
            or "markdor_save_current_document"
            or "markdor_ui_copy_rendered_html";
    }

    private string GetCurrentTargetName()
    {
        var document = _shell.GetCurrentDocumentSnapshot();
        return document.FilePath ?? document.FileName;
    }

    private AppStatusResult GetAppStatus()
    {
        return new AppStatusResult(
            Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown",
            _shell.Appearance.SelectedTheme?.Key,
            _shell.Appearance.SelectedTypography?.Key,
            _shell.DocumentInfo.CurrentFilePath,
            _shell.DocumentInfo.IsModified);
    }

    private OperationAuditResult GetOperationAudit()
    {
        return new OperationAuditResult(_auditService.GetRecent());
    }

    private UiStateResult GetUiState()
    {
        return new UiStateResult(
            _shell.Appearance.SelectedTheme?.Key,
            _shell.Appearance.SelectedTypography?.Key,
            _shell.Appearance.SelectedLanguage?.CultureName,
            _shell.Appearance.IsCompactLayout,
            _shell.Layout.IsSidebarVisible,
            _shell.Navigation.SelectedSideTabIndex == 1 ? "outline" : "files",
            _shell.Layout.IsPreviewVisible,
            _shell.Layout.IsSourceMode,
            _shell.EditorDisplay.ShowLineNumbers,
            _shell.Layout.IsStatusBarVisible);
    }

    private OperationResult SetTheme(UiSetThemeInput input)
    {
        _shell.Appearance.SelectThemeByKey(input.ThemeKey);
        return new OperationResult("ok", "theme updated");
    }

    private OperationResult SetTypography(UiSetTypographyInput input)
    {
        _shell.Appearance.SelectTypographyByKey(input.TypographyKey);
        return new OperationResult("ok", "typography updated");
    }

    private OperationResult SetLanguage(UiSetLanguageInput input)
    {
        _shell.Appearance.SelectLanguageByCulture(input.CultureName);
        return new OperationResult("ok", "language updated");
    }

    private OperationResult SetLayout(UiSetLayoutInput input)
    {
        if (input.SidebarVisible is { } sidebar)
        {
            _shell.Layout.IsSidebarVisible = sidebar;
        }

        if (input.StatusBarVisible is { } statusBar)
        {
            _shell.Layout.IsStatusBarVisible = statusBar;
        }

        if (input.PreviewVisible is { } preview)
        {
            _shell.Layout.IsPreviewVisible = preview;
        }

        if (input.LineNumbersVisible is { } lineNumbers)
        {
            _shell.EditorDisplay.ShowLineNumbers = lineNumbers;
        }

        if (input.CompactLayout is { } compact)
        {
            _shell.Appearance.IsCompactLayout = compact;
        }

        if (input.SourceMode is { } sourceMode && sourceMode != _shell.Layout.IsSourceMode)
        {
            if (_shell.Workbench.IsSourceEditing) _shell.Workbench.ShowInstant();
            else _shell.Workbench.ShowSource();
        }

        return new OperationResult("ok", "layout updated");
    }

    private OperationResult ShowSidebarTab(UiShowSidebarTabInput input)
    {
        if (input.Tab.Equals("outline", StringComparison.OrdinalIgnoreCase))
        {
            _shell.Layout.ShowOutline();
        }
        else
        {
            _shell.Layout.ShowFiles();
        }

        return new OperationResult("ok", "sidebar tab updated");
    }

    private async Task<OperationResult> OpenPanelAsync(UiOpenPanelInput input)
    {
        switch (NormalizeToolToken(input.Panel))
        {
            case "find":
                _shell.ShowFindPanel();
                break;
            case "replace":
                _shell.ShowReplacePanel();
                break;
            case "properties":
                _shell.ShowProperties();
                break;
            case "wordcount":
                _shell.WordCount();
                break;
            case "changelog":
                await _shell.Help.OpenHelpTopic("changelog");
                break;
            case "thanks":
                await _shell.Help.OpenHelpTopic("thanks");
                break;
            case "about":
                await _shell.Help.OpenHelpTopic("about");
                break;
            case "mcpsettings":
                CodeWF.Toolkit.EventBus.EventBus.Default.Publish(new ShellActionCommand(ShellActionKind.ShowMcpSettings));
                break;
            default:
                throw new InvalidOperationException($"Unsupported panel: {input.Panel}");
        }

        return new OperationResult("ok", "panel opened");
    }

    private async Task<OperationResult> ExportCurrentDocumentAsync(ExportCurrentDocumentInput input)
    {
        if (NormalizeToolToken(input.Format) is not ("html" or "pdf" or "png" or "word"))
        {
            throw new InvalidOperationException($"Unsupported export format: {input.Format}");
        }

        await _shell.Export(input.Format);
        return new OperationResult("ok", "export requested");
    }

    private async Task<OperationResult> CopyRenderedHtmlAsync(CopyRenderedHtmlInput input)
    {
        if (NormalizeToolToken(input.Target) is not ("wechat" or "zhihu" or "juejin"))
        {
            throw new InvalidOperationException($"Unsupported copy target: {input.Target}");
        }

        if (!await ConfirmIfRequiredAsync("markdor_ui_copy_rendered_html", "clipboard", $"copy rendered HTML for {input.Target}"))
        {
            return new OperationResult("canceled", "operation rejected");
        }

        await _shell.CopyHtml(input.Target);
        return new OperationResult("ok", "rendered HTML copied");
    }

    private static OperationResult ApplyEditorCommand(UiApplyEditorCommandInput input)
    {
        if (!TryMapEditorAction(input.Command, out var action))
        {
            throw new InvalidOperationException($"Unsupported editor command: {input.Command}");
        }

        CodeWF.Toolkit.EventBus.EventBus.Default.Publish(new EditorActionCommand(action));
        return new OperationResult("ok", "editor command applied");
    }

    private static bool TryMapEditorAction(string command, out EditorActionKind action)
    {
        var normalized = NormalizeToolToken(command);
        action = normalized switch
        {
            "undo" => EditorActionKind.Undo,
            "redo" => EditorActionKind.Redo,
            "copy" => EditorActionKind.Copy,
            "selectall" => EditorActionKind.SelectAll,
            "paragraph" => EditorActionKind.Paragraph,
            "heading1" => EditorActionKind.Heading1,
            "heading2" => EditorActionKind.Heading2,
            "heading3" => EditorActionKind.Heading3,
            "bold" => EditorActionKind.Bold,
            "italic" => EditorActionKind.Italic,
            "inlinecode" => EditorActionKind.InlineCode,
            "link" => EditorActionKind.Link,
            "image" => EditorActionKind.Image,
            "quote" => EditorActionKind.Quote,
            "orderedlist" => EditorActionKind.OrderedList,
            "unorderedlist" => EditorActionKind.UnorderedList,
            "tasklist" => EditorActionKind.TaskList,
            "codefence" => EditorActionKind.CodeFence,
            "mathblock" => EditorActionKind.MathBlock,
            "table" => EditorActionKind.Table,
            "horizontalrule" => EditorActionKind.HorizontalRule,
            "clearformatting" => EditorActionKind.ClearFormatting,
            _ => default
        };
        return normalized is "undo" or "redo" or "copy" or "selectall" or "paragraph"
            or "heading1" or "heading2" or "heading3" or "bold" or "italic" or "inlinecode"
            or "link" or "image" or "quote" or "orderedlist" or "unorderedlist" or "tasklist"
            or "codefence" or "mathblock" or "table" or "horizontalrule" or "clearformatting";
    }

    private static T Read<T>(JsonElement? arguments, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo)
    {
        if (arguments is null || arguments.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return JsonSerializer.Deserialize("{}", typeInfo)!;
        }

        return arguments.Value.Deserialize(typeInfo)!;
    }

    private static McpToolCallResult TextResult(string text, bool isError = false)
    {
        return new McpToolCallResult([new McpContentItem("text", text)], isError);
    }

    private static string SerializeToolResult(object result)
    {
        return result switch
        {
            CurrentDocumentResult value => JsonSerializer.Serialize(value, McpJsonContext.Default.CurrentDocumentResult),
            OutlineResult value => JsonSerializer.Serialize(value, McpJsonContext.Default.OutlineResult),
            SelectionResult value => JsonSerializer.Serialize(value, McpJsonContext.Default.SelectionResult),
            AppStatusResult value => JsonSerializer.Serialize(value, McpJsonContext.Default.AppStatusResult),
            UiStateResult value => JsonSerializer.Serialize(value, McpJsonContext.Default.UiStateResult),
            RenderedHtmlResult value => JsonSerializer.Serialize(value, McpJsonContext.Default.RenderedHtmlResult),
            OperationResult value => JsonSerializer.Serialize(value, McpJsonContext.Default.OperationResult),
            OperationAuditResult value => JsonSerializer.Serialize(value, McpJsonContext.Default.OperationAuditResult),
            ListFilesResult value => JsonSerializer.Serialize(value, McpJsonContext.Default.ListFilesResult),
            _ => JsonSerializer.Serialize(new OperationResult("ok", result.ToString()), McpJsonContext.Default.OperationResult)
        };
    }

    private static McpToolDescription Tool(string name, string description, string inputSchema = EmptySchema)
    {
        using var document = JsonDocument.Parse(inputSchema);
        return new McpToolDescription(name, description, document.RootElement.Clone());
    }

    private static string NormalizeToolName(string name)
    {
        return name.Replace('.', '_');
    }

    private static string NormalizeToolToken(string value)
    {
        return value.Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
    }

    private static string GetEncodingDisplayName(Encoding encoding)
    {
        if (encoding is UTF8Encoding { Preamble.Length: > 0 })
        {
            return "UTF-8 BOM";
        }

        if (encoding.CodePage == Encoding.UTF8.CodePage)
        {
            return "UTF-8";
        }

        return encoding.WebName.ToUpperInvariant();
    }
}
