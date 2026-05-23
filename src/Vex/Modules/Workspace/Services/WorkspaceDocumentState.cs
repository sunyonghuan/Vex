using CodeWF.EventBus;
using Vex.Core.Messaging;
using Vex.Core.Services;

namespace Vex.Modules.Workspace.Services;

public sealed class WorkspaceDocumentState : IWorkspaceDocumentState
{
    private readonly IEventBus _eventBus;
    private string _markdown = string.Empty;
    private string? _filePath;

    public WorkspaceDocumentState(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public string Markdown => _markdown;

    public string? FilePath => _filePath;

    public void UpdateDocument(string markdown, string? filePath)
    {
        var normalized = markdown ?? string.Empty;
        var normalizedPath = string.IsNullOrWhiteSpace(filePath) ? null : filePath;
        if (_markdown == normalized && string.Equals(_filePath, normalizedPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _markdown = normalized;
        _filePath = normalizedPath;
        // 文档正文变化统一广播，预览、大纲和后续可视化编辑都可以复用这一条轻量状态通道。
        _eventBus.Publish(new MarkdownDocumentChangedCommand(_markdown, _filePath));
    }
}
