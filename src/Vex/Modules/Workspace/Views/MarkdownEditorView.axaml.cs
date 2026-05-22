using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaEdit.Highlighting;
using CodeWF.EventBus;
using Prism.Ioc;
using Vex.Core.Messaging;
using Vex.Modules.Shell.ViewModels;
using Vex.Modules.Workspace.Services;

namespace Vex.Modules.Workspace.Views;

public partial class MarkdownEditorView : UserControl
{
    private MainWindowViewModel? _viewModel;
    private IMarkdownEditorController? _editorController;
    private IEventBus? _eventBus;

    public MarkdownEditorView()
    {
        InitializeComponent();
        MarkdownEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("MarkDown");
        ConfigureEditorVisuals();
        MarkdownEditor.AddHandler(InputElement.KeyDownEvent, OnEditorKeyDown, RoutingStrategies.Tunnel);
        DataContextChanged += (_, _) => AttachViewModel(DataContext as MainWindowViewModel);
        AttachedToVisualTree += (_, _) =>
        {
            AttachEditorController();
            SyncEditorFromViewModel();
        };
        DetachedFromVisualTree += (_, _) => DetachEditorController();
        AttachViewModel(DataContext as MainWindowViewModel);
    }

    private void ConfigureEditorVisuals()
    {
        MarkdownEditor.Options.HighlightCurrentLine = true;
        MarkdownEditor.TextArea.TextView.CurrentLineBackground = new SolidColorBrush(Color.Parse("#FFF4F7FB"));
        MarkdownEditor.TextArea.TextView.CurrentLineBorder = new Pen(new SolidColorBrush(Color.Parse("#FFE4E9F2")), 1);
    }

    private void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Tab)
        {
            return;
        }

        // Tab/Shift+Tab 只发布编辑动作，缩进文本如何变化由 Workspace 控制器统一维护。
        e.Handled = true;
        PublishEditorAction(e.KeyModifiers.HasFlag(KeyModifiers.Shift)
            ? EditorActionKind.Outdent
            : EditorActionKind.Indent);
    }

    private void AttachViewModel(MainWindowViewModel? viewModel)
    {
        if (ReferenceEquals(_viewModel, viewModel))
        {
            AttachEditorController();
            SyncEditorFromViewModel();
            return;
        }

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        DetachEditorController();
        _viewModel = viewModel;

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        AttachEditorController();
        SyncEditorFromViewModel();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainWindowViewModel.Markdown) or null)
        {
            SyncEditorFromViewModel();
        }
    }

    private void SyncEditorFromViewModel()
    {
        if (_viewModel is null)
        {
            return;
        }

        _editorController?.SyncText(_viewModel.Markdown);
    }

    private void AttachEditorController()
    {
        if (Design.IsDesignMode)
        {
            return;
        }

        _editorController ??= (IMarkdownEditorController)ContainerLocator.Container.Resolve(typeof(IMarkdownEditorController));
        _editorController.Attach(MarkdownEditor);
    }

    private void PublishEditorAction(EditorActionKind action)
    {
        if (Design.IsDesignMode)
        {
            return;
        }

        _eventBus ??= (IEventBus)ContainerLocator.Container.Resolve(typeof(IEventBus));
        _eventBus.Publish(new EditorActionCommand(action));
    }

    private void DetachEditorController()
    {
        _editorController?.Detach(MarkdownEditor);
    }
}
