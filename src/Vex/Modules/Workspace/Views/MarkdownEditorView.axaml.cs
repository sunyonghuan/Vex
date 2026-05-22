using System.ComponentModel;
using Avalonia.Controls;
using AvaloniaEdit.Highlighting;
using CodeWF.EventBus;
using Vex.Core.Messaging;
using Vex.Modules.Shell.ViewModels;

namespace Vex.Modules.Workspace.Views;

public partial class MarkdownEditorView : UserControl
{
    private MainWindowViewModel? _viewModel;
    private IEventBus? _eventBus;
    private bool _syncingEditor;

    public MarkdownEditorView()
    {
        InitializeComponent();
        MarkdownEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("MarkDown");
        MarkdownEditor.TextChanged += (_, _) => PublishEditorText();
        DataContextChanged += (_, _) => AttachViewModel(DataContext as MainWindowViewModel);
        AttachViewModel(DataContext as MainWindowViewModel);
    }

    private void AttachViewModel(MainWindowViewModel? viewModel)
    {
        if (ReferenceEquals(_viewModel, viewModel))
        {
            SyncEditorFromViewModel();
            return;
        }

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (_eventBus is not null)
        {
            _eventBus.Unsubscribe<EditorActionCommand>(OnEditorAction);
            _eventBus.Unsubscribe<NavigateToLineCommand>(OnNavigateToLine);
        }

        _viewModel = viewModel;
        _eventBus = viewModel?.EventBus;

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        if (_eventBus is not null)
        {
            _eventBus.Subscribe<EditorActionCommand>(OnEditorAction);
            _eventBus.Subscribe<NavigateToLineCommand>(OnNavigateToLine);
        }

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
        if (_viewModel is null || MarkdownEditor.Text == _viewModel.Markdown)
        {
            return;
        }

        _syncingEditor = true;
        MarkdownEditor.Text = _viewModel.Markdown;
        _syncingEditor = false;
    }

    private void PublishEditorText()
    {
        if (_syncingEditor || _eventBus is null)
        {
            return;
        }

        var caret = MarkdownEditor.TextArea.Caret;
        _eventBus.Publish(new MarkdownTextChangedCommand(
            MarkdownEditor.Text ?? string.Empty,
            caret.Line,
            caret.Column));
    }

    private void OnEditorAction(EditorActionCommand command)
    {
        switch (command.Action)
        {
            case EditorActionKind.Undo:
                MarkdownEditor.Undo();
                break;
            case EditorActionKind.Redo:
                MarkdownEditor.Redo();
                break;
            case EditorActionKind.Cut:
                MarkdownEditor.Cut();
                break;
            case EditorActionKind.Copy:
                MarkdownEditor.Copy();
                break;
            case EditorActionKind.Paste:
                MarkdownEditor.Paste();
                break;
            case EditorActionKind.SelectAll:
                MarkdownEditor.SelectAll();
                break;
            case EditorActionKind.Bold:
                WrapSelection("**", "**", "bold text");
                break;
            case EditorActionKind.Italic:
                WrapSelection("*", "*", "italic text");
                break;
            case EditorActionKind.InlineCode:
                WrapSelection("`", "`", "code");
                break;
            case EditorActionKind.Link:
                WrapSelection("[", "](https://example.com)", "link text");
                break;
            case EditorActionKind.Image:
                InsertText("![alt text](image.png)");
                break;
            case EditorActionKind.Paragraph:
                PrefixCurrentLine(string.Empty);
                break;
            case EditorActionKind.Heading1:
                PrefixCurrentLine("# ");
                break;
            case EditorActionKind.Heading2:
                PrefixCurrentLine("## ");
                break;
            case EditorActionKind.Heading3:
                PrefixCurrentLine("### ");
                break;
            case EditorActionKind.Heading4:
                PrefixCurrentLine("#### ");
                break;
            case EditorActionKind.Heading5:
                PrefixCurrentLine("##### ");
                break;
            case EditorActionKind.Heading6:
                PrefixCurrentLine("###### ");
                break;
            case EditorActionKind.Quote:
                PrefixCurrentLine("> ");
                break;
            case EditorActionKind.UnorderedList:
                PrefixCurrentLine("- ");
                break;
            case EditorActionKind.OrderedList:
                PrefixCurrentLine("1. ");
                break;
            case EditorActionKind.TaskList:
                PrefixCurrentLine("- [ ] ");
                break;
            case EditorActionKind.CodeFence:
                WrapSelection("```csharp\n", "\n```", "Console.WriteLine(\"Vex\");");
                break;
            case EditorActionKind.Table:
                InsertText("\n| Column | Value |\n| --- | --- |\n| Item | Description |\n");
                break;
            case EditorActionKind.MathBlock:
                WrapSelection("$$\n", "\n$$", "E = mc^2");
                break;
            case EditorActionKind.HorizontalRule:
                InsertText("\n---\n");
                break;
            case EditorActionKind.FocusEditor:
                MarkdownEditor.Focus();
                break;
        }
    }

    private void OnNavigateToLine(NavigateToLineCommand command)
    {
        if (MarkdownEditor.Document is null)
        {
            return;
        }

        var line = Math.Clamp(command.Line, 1, MarkdownEditor.Document.LineCount);
        var offset = MarkdownEditor.Document.GetLineByNumber(line).Offset;
        MarkdownEditor.CaretOffset = offset;
        MarkdownEditor.TextArea.Caret.BringCaretToView();
        MarkdownEditor.Focus();
        PublishEditorText();
    }

    private void WrapSelection(string prefix, string suffix, string placeholder)
    {
        var text = MarkdownEditor.Text ?? string.Empty;
        var start = Math.Clamp(MarkdownEditor.SelectionStart, 0, text.Length);
        var length = Math.Clamp(MarkdownEditor.SelectionLength, 0, text.Length - start);
        var selected = length > 0 ? text.Substring(start, length) : placeholder;
        var replacement = $"{prefix}{selected}{suffix}";
        MarkdownEditor.Text = text[..start] + replacement + text[(start + length)..];
        MarkdownEditor.SelectionStart = start + prefix.Length;
        MarkdownEditor.SelectionLength = selected.Length;
        MarkdownEditor.CaretOffset = start + replacement.Length;
        PublishEditorText();
    }

    private void InsertText(string insertion)
    {
        var text = MarkdownEditor.Text ?? string.Empty;
        var start = Math.Clamp(MarkdownEditor.SelectionStart, 0, text.Length);
        var length = Math.Clamp(MarkdownEditor.SelectionLength, 0, text.Length - start);
        MarkdownEditor.Text = text[..start] + insertion + text[(start + length)..];
        MarkdownEditor.CaretOffset = start + insertion.Length;
        PublishEditorText();
    }

    private void PrefixCurrentLine(string prefix)
    {
        var text = MarkdownEditor.Text ?? string.Empty;
        var offset = Math.Clamp(MarkdownEditor.CaretOffset, 0, text.Length);
        var lineStart = text.LastIndexOf('\n', Math.Max(0, offset - 1));
        lineStart = lineStart < 0 ? 0 : lineStart + 1;
        var lineEnd = text.IndexOf('\n', lineStart);
        if (lineEnd < 0)
        {
            lineEnd = text.Length;
        }

        var line = text[lineStart..lineEnd];
        var normalized = RemoveMarkdownLinePrefix(line);
        var replacement = string.IsNullOrEmpty(prefix) ? normalized : prefix + normalized;
        MarkdownEditor.Text = text[..lineStart] + replacement + text[lineEnd..];
        MarkdownEditor.CaretOffset = lineStart + replacement.Length;
        PublishEditorText();
    }

    private static string RemoveMarkdownLinePrefix(string line)
    {
        var trimmed = line.TrimStart();
        var leading = line.Length - trimmed.Length;

        string[] prefixes =
        [
            "###### ",
            "##### ",
            "#### ",
            "### ",
            "## ",
            "# ",
            "> ",
            "- [ ] ",
            "- ",
            "1. "
        ];

        foreach (var prefix in prefixes)
        {
            if (trimmed.StartsWith(prefix, StringComparison.Ordinal))
            {
                return line[..leading] + trimmed[prefix.Length..];
            }
        }

        return line;
    }
}
