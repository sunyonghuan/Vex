using AvaloniaEdit;
using Vex.Core.Messaging;

namespace Vex.Modules.Workspace.Services;

public sealed class MarkdownEditorActionService : IMarkdownEditorActionService
{
    private readonly IMarkdownEditorMutationService _textMutationService;

    public MarkdownEditorActionService(IMarkdownEditorMutationService textMutationService)
    {
        _textMutationService = textMutationService;
    }

    public void Execute(TextEditor editor, EditorActionKind action, Action<Action> runTextMutation)
    {
        // 这里只维护“用户动作 -> 编辑器变更”的映射，事件发布和同步节流仍由控制器统一处理。
        switch (action)
        {
            case EditorActionKind.Undo:
                runTextMutation(() => editor.Undo());
                break;
            case EditorActionKind.Redo:
                runTextMutation(() => editor.Redo());
                break;
            case EditorActionKind.Cut:
                runTextMutation(editor.Cut);
                break;
            case EditorActionKind.Copy:
                editor.Copy();
                break;
            case EditorActionKind.Paste:
                runTextMutation(editor.Paste);
                break;
            case EditorActionKind.SelectAll:
                editor.SelectAll();
                break;
            case EditorActionKind.Bold:
                WrapSelection(editor, "**", "**", "bold text", runTextMutation);
                break;
            case EditorActionKind.Italic:
                WrapSelection(editor, "*", "*", "italic text", runTextMutation);
                break;
            case EditorActionKind.InlineCode:
                WrapSelection(editor, "`", "`", "code", runTextMutation);
                break;
            case EditorActionKind.Link:
                WrapSelection(editor, "[", "](https://example.com)", "link text", runTextMutation);
                break;
            case EditorActionKind.Image:
                InsertText(editor, "![alt text](image.png)", runTextMutation);
                break;
            case EditorActionKind.ClearFormatting:
                MutateEditor(editor, _textMutationService.ClearFormatting, runTextMutation);
                break;
            case EditorActionKind.Paragraph:
                PrefixCurrentLine(editor, string.Empty, runTextMutation);
                break;
            case EditorActionKind.Heading1:
                PrefixCurrentLine(editor, "# ", runTextMutation);
                break;
            case EditorActionKind.Heading2:
                PrefixCurrentLine(editor, "## ", runTextMutation);
                break;
            case EditorActionKind.Heading3:
                PrefixCurrentLine(editor, "### ", runTextMutation);
                break;
            case EditorActionKind.Heading4:
                PrefixCurrentLine(editor, "#### ", runTextMutation);
                break;
            case EditorActionKind.Heading5:
                PrefixCurrentLine(editor, "##### ", runTextMutation);
                break;
            case EditorActionKind.Heading6:
                PrefixCurrentLine(editor, "###### ", runTextMutation);
                break;
            case EditorActionKind.Quote:
                PrefixCurrentLine(editor, "> ", runTextMutation);
                break;
            case EditorActionKind.UnorderedList:
                PrefixCurrentLine(editor, "- ", runTextMutation);
                break;
            case EditorActionKind.OrderedList:
                PrefixCurrentLine(editor, "1. ", runTextMutation);
                break;
            case EditorActionKind.TaskList:
                PrefixCurrentLine(editor, "- [ ] ", runTextMutation);
                break;
            case EditorActionKind.CodeFence:
                WrapSelection(editor, "```csharp\n", "\n```", "Console.WriteLine(\"Vex\");", runTextMutation);
                break;
            case EditorActionKind.Table:
                InsertText(editor, "\n| Column | Value |\n| --- | --- |\n| Item | Description |\n", runTextMutation);
                break;
            case EditorActionKind.MathBlock:
                WrapSelection(editor, "$$\n", "\n$$", "E = mc^2", runTextMutation);
                break;
            case EditorActionKind.HorizontalRule:
                InsertText(editor, "\n---\n", runTextMutation);
                break;
            case EditorActionKind.Indent:
                MutateEditor(editor, _textMutationService.IndentSelection, runTextMutation);
                break;
            case EditorActionKind.Outdent:
                MutateEditor(editor, _textMutationService.OutdentSelection, runTextMutation);
                break;
            case EditorActionKind.FocusEditor:
                editor.Focus();
                break;
        }
    }

    private void WrapSelection(
        TextEditor editor,
        string prefix,
        string suffix,
        string placeholder,
        Action<Action> runTextMutation)
    {
        MutateEditor(
            editor,
            currentEditor => _textMutationService.WrapSelection(currentEditor, prefix, suffix, placeholder),
            runTextMutation);
    }

    private void InsertText(TextEditor editor, string insertion, Action<Action> runTextMutation)
    {
        MutateEditor(editor, currentEditor => _textMutationService.InsertText(currentEditor, insertion), runTextMutation);
    }

    private void PrefixCurrentLine(TextEditor editor, string prefix, Action<Action> runTextMutation)
    {
        MutateEditor(editor, currentEditor => _textMutationService.PrefixCurrentLine(currentEditor, prefix), runTextMutation);
    }

    private static void MutateEditor(TextEditor editor, Action<TextEditor> mutation, Action<Action> runTextMutation)
    {
        runTextMutation(() => mutation(editor));
    }
}
