using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Vex.Modules.Workspace.ViewModels;

namespace Vex.Modules.Workspace.Views;

public partial class MarkdownPreviewView : UserControl
{
    private MarkdownPreviewViewModel? _viewModel;

    public MarkdownPreviewView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        AttachedToVisualTree += (_, _) => SetViewModel(DataContext as MarkdownPreviewViewModel);
        DetachedFromVisualTree += (_, _) => SetViewModel(null);
        SetViewModel(DataContext as MarkdownPreviewViewModel);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        SetViewModel(DataContext as MarkdownPreviewViewModel);
    }

    private void SetViewModel(MarkdownPreviewViewModel? viewModel)
    {
        if (ReferenceEquals(_viewModel, viewModel))
        {
            return;
        }

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = viewModel;

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            QueueScrollToEditorPosition();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MarkdownPreviewViewModel.PreviewSourceLine)
            or nameof(MarkdownPreviewViewModel.PreviewScrollRatio)
            or nameof(MarkdownPreviewViewModel.Markdown))
        {
            QueueScrollToEditorPosition();
        }
    }

    private void QueueScrollToEditorPosition()
    {
        Dispatcher.UIThread.Post(ScrollToEditorPosition, DispatcherPriority.Background);
    }

    private void ScrollToEditorPosition()
    {
        if (_viewModel is null)
        {
            return;
        }

        if (TryScrollToSourceLine())
        {
            return;
        }

        var scrollableHeight = Math.Max(0d, PreviewScrollViewer.Extent.Height - PreviewScrollViewer.Viewport.Height);
        var targetY = scrollableHeight * Math.Clamp(_viewModel.PreviewScrollRatio, 0d, 1d);
        PreviewScrollViewer.Offset = new Vector(0d, targetY);
    }

    private bool TryScrollToSourceLine()
    {
        if (_viewModel is null || !PreviewMarkdownViewer.TryGetSourceLineBounds(_viewModel.PreviewSourceLine, out var bounds))
        {
            return false;
        }

        var scrollableHeight = Math.Max(0d, PreviewScrollViewer.Extent.Height - PreviewScrollViewer.Viewport.Height);
        if (scrollableHeight <= 0d)
        {
            return true;
        }

        var targetY = Math.Clamp(bounds.Y, 0d, scrollableHeight);
        PreviewScrollViewer.Offset = new Vector(0d, targetY);
        return true;
    }
}
