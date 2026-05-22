using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaEdit.Highlighting;
using Vex.Modules.Workspace.ViewModels;

namespace Vex.Modules.Workspace.Views;

public partial class MarkdownEditorView : UserControl
{
    public MarkdownEditorView()
    {
        InitializeComponent();
        MarkdownEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("MarkDown");
        ConfigureEditorVisuals();
        MarkdownEditor.AddHandler(InputElement.KeyDownEvent, OnEditorKeyDown, RoutingStrategies.Tunnel);
        DataContextChanged += (_, _) => AttachEditorController();
        AttachedToVisualTree += (_, _) => AttachEditorController();
        DetachedFromVisualTree += (_, _) => ViewModel?.DetachEditor(MarkdownEditor);
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
        ViewModel?.HandleEditorKeyDown(e.Key, e.KeyModifiers);
    }

    private void AttachEditorController()
    {
        ViewModel?.AttachEditor(MarkdownEditor);
    }

    private MarkdownEditorViewModel? ViewModel => DataContext as MarkdownEditorViewModel;
}
