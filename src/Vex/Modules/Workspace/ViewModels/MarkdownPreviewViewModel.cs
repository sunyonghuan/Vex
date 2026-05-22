using CodeWF.EventBus;
using ReactiveUI;
using Vex.Core.Messaging;
using Vex.Core.Services;

namespace Vex.Modules.Workspace.ViewModels;

public sealed class MarkdownPreviewViewModel : ReactiveObject
{
    private readonly IEventBus _eventBus;
    private readonly IEditorAppearanceState _appearanceState;
    private readonly IMarkdownVisualEditorState _visualEditorState;
    private string _markdown;
    private string _typographySize;
    private string? _typographyTheme;
    private bool _allowEdit;

    public MarkdownPreviewViewModel(
        IEventBus eventBus,
        IWorkspaceDocumentState documentState,
        IEditorAppearanceState appearanceState,
        IMarkdownVisualEditorState visualEditorState)
    {
        _eventBus = eventBus;
        _appearanceState = appearanceState;
        _visualEditorState = visualEditorState;
        _markdown = documentState.Markdown;
        _typographySize = appearanceState.TypographySize;
        _typographyTheme = appearanceState.TypographyTheme;
        _allowEdit = visualEditorState.AllowPreviewEdit;
        _appearanceState.Changed += OnAppearanceChanged;
        _visualEditorState.Changed += OnVisualEditorStateChanged;
        eventBus.Subscribe(this);
    }

    public string Markdown
    {
        get => _markdown;
        private set => this.RaiseAndSetIfChanged(ref _markdown, value);
    }

    public string TypographySize
    {
        get => _typographySize;
        private set => this.RaiseAndSetIfChanged(ref _typographySize, value);
    }

    public string? TypographyTheme
    {
        get => _typographyTheme;
        private set => this.RaiseAndSetIfChanged(ref _typographyTheme, value);
    }

    public bool AllowEdit
    {
        get => _allowEdit;
        private set => this.RaiseAndSetIfChanged(ref _allowEdit, value);
    }

    public void ApplyVisualMarkdownEdit(string markdown)
    {
        if (Markdown == markdown)
        {
            return;
        }

        // 可视化编辑复用源码编辑器同一条 MarkdownTextChanged 通道，保证保存状态、统计和大纲同步。
        _eventBus.Publish(new MarkdownTextChangedCommand(markdown, 1, 1));
    }

    [EventHandler]
    public void ApplyMarkdownDocumentChanged(MarkdownDocumentChangedCommand command)
    {
        Markdown = command.Markdown;
    }

    private void OnAppearanceChanged(object? sender, EventArgs e)
    {
        TypographySize = _appearanceState.TypographySize;
        TypographyTheme = _appearanceState.TypographyTheme;
    }

    private void OnVisualEditorStateChanged(object? sender, EventArgs e)
    {
        AllowEdit = _visualEditorState.AllowPreviewEdit;
    }
}
