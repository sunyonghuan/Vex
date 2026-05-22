using AvaloniaEdit;
using CodeWF.EventBus;
using Vex.Core.Messaging;
using Vex.Core.Services;

namespace Vex.Modules.Workspace.Services;

public sealed class MarkdownEditorController : IMarkdownEditorController
{
    private readonly IEventBus _eventBus;
    private readonly IMarkdownEditorMutationService _textMutationService;
    private readonly IMarkdownEditorSearchService _searchService;
    private TextEditor? _editor;
    private bool _suppressTextChanged;

    public MarkdownEditorController(
        IEventBus eventBus,
        IMarkdownEditorMutationService textMutationService,
        IMarkdownEditorSearchService searchService)
    {
        _eventBus = eventBus;
        _textMutationService = textMutationService;
        _searchService = searchService;
        eventBus.Subscribe(this);
    }

    public void Attach(TextEditor editor)
    {
        if (ReferenceEquals(_editor, editor))
        {
            return;
        }

        DetachCurrentEditor();
        _editor = editor;
        _editor.TextChanged += OnEditorTextChanged;
        _editor.TextArea.Caret.PositionChanged += OnCaretPositionChanged;
    }

    public void Detach(TextEditor editor)
    {
        if (ReferenceEquals(_editor, editor))
        {
            DetachCurrentEditor();
        }
    }

    public void SyncText(string? markdown)
    {
        if (_editor is null)
        {
            return;
        }

        var normalized = markdown ?? string.Empty;
        if (_editor.Text == normalized)
        {
            return;
        }

        _suppressTextChanged = true;
        try
        {
            _editor.Text = normalized;
        }
        finally
        {
            _suppressTextChanged = false;
        }
    }

    public void PublishTextChanged()
    {
        if (_editor is null)
        {
            return;
        }

        var caret = _editor.TextArea.Caret;
        _eventBus.Publish(new MarkdownTextChangedCommand(
            _editor.Text ?? string.Empty,
            caret.Line,
            caret.Column));
    }

    [EventHandler]
    public void Execute(EditorActionCommand command)
    {
        if (_editor is null)
        {
            return;
        }

        switch (command.Action)
        {
            case EditorActionKind.Undo:
                RunTextMutation(() => _editor.Undo());
                break;
            case EditorActionKind.Redo:
                RunTextMutation(() => _editor.Redo());
                break;
            case EditorActionKind.Cut:
                RunTextMutation(_editor.Cut);
                break;
            case EditorActionKind.Copy:
                _editor.Copy();
                break;
            case EditorActionKind.Paste:
                RunTextMutation(_editor.Paste);
                break;
            case EditorActionKind.SelectAll:
                _editor.SelectAll();
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
            case EditorActionKind.ClearFormatting:
                ClearFormatting();
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
            case EditorActionKind.Indent:
                IndentSelection();
                break;
            case EditorActionKind.Outdent:
                OutdentSelection();
                break;
            case EditorActionKind.FocusEditor:
                _editor.Focus();
                break;
        }
    }

    [EventHandler]
    public void Search(EditorSearchCommand command)
    {
        if (_editor is null)
        {
            return;
        }

        _searchService.Search(_editor, command, RunTextMutation, PublishTextChanged);
    }

    [EventHandler]
    public void NavigateTo(NavigateToLineCommand command)
    {
        if (_editor?.Document is null)
        {
            return;
        }

        var line = Math.Clamp(command.Line, 1, _editor.Document.LineCount);
        var offset = _editor.Document.GetLineByNumber(line).Offset;
        _editor.CaretOffset = offset;
        _editor.TextArea.Caret.BringCaretToView();
        _editor.Focus();
        PublishTextChanged();
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        if (!_suppressTextChanged)
        {
            PublishTextChanged();
        }
    }

    private void OnCaretPositionChanged(object? sender, EventArgs e)
    {
        if (!_suppressTextChanged)
        {
            // 光标移动也走同一条消息通道，确保状态栏行列号不依赖文本变化才刷新。
            PublishTextChanged();
        }
    }

    private void RunTextMutation(Action mutation)
    {
        _suppressTextChanged = true;
        try
        {
            mutation();
        }
        finally
        {
            _suppressTextChanged = false;
        }

        PublishTextChanged();
    }

    private void WrapSelection(string prefix, string suffix, string placeholder)
    {
        MutateEditor(editor => _textMutationService.WrapSelection(editor, prefix, suffix, placeholder));
    }

    private void InsertText(string insertion)
    {
        MutateEditor(editor => _textMutationService.InsertText(editor, insertion));
    }

    private void IndentSelection()
    {
        MutateEditor(_textMutationService.IndentSelection);
    }

    private void OutdentSelection()
    {
        MutateEditor(_textMutationService.OutdentSelection);
    }

    private void ClearFormatting()
    {
        MutateEditor(_textMutationService.ClearFormatting);
    }

    private void PrefixCurrentLine(string prefix)
    {
        MutateEditor(editor => _textMutationService.PrefixCurrentLine(editor, prefix));
    }

    private void DetachCurrentEditor()
    {
        if (_editor is not null)
        {
            _editor.TextChanged -= OnEditorTextChanged;
            _editor.TextArea.Caret.PositionChanged -= OnCaretPositionChanged;
            _editor = null;
        }
    }

    private void MutateEditor(Action<TextEditor> mutation)
    {
        if (_editor is null)
        {
            return;
        }

        var editor = _editor;
        RunTextMutation(() => mutation(editor));
    }
}
