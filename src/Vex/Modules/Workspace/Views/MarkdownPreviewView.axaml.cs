using Avalonia.Controls;
using CodeWF.Markdown.Controls;
using Vex.Modules.Workspace.ViewModels;

namespace Vex.Modules.Workspace.Views;

public partial class MarkdownPreviewView : UserControl
{
    public MarkdownPreviewView()
    {
        InitializeComponent();
    }

    private void OnMarkdownEdited(object? sender, MarkdownEditedEventArgs e)
    {
        if (DataContext is MarkdownPreviewViewModel viewModel)
        {
            viewModel.ApplyVisualMarkdownEdit(e.Markdown);
        }
    }
}
