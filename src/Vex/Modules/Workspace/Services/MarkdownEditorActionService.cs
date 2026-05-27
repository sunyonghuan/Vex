using System.Text;

using Avalonia.Controls;
using Avalonia.Input.Platform;
using AvaloniaEdit;
using CodeWF.Markdown;
using Vex.Core.Messaging;

namespace Vex.Modules.Workspace.Services;

public sealed class MarkdownEditorActionService : IMarkdownEditorActionService
{
    private readonly IMarkdownEditorTemplateService _templates;
    private readonly IMarkdownEditorMutationService _textMutationService;

    public MarkdownEditorActionService(
        IMarkdownEditorTemplateService templates,
        IMarkdownEditorMutationService textMutationService)
    {
        _templates = templates;
        _textMutationService = textMutationService;
    }

    public async Task ExecuteAsync(TextEditor editor, EditorActionKind action, Action<Action> runTextMutation)
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
                if (!await TryPasteHtmlAsMarkdownAsync(editor, runTextMutation))
                {
                    runTextMutation(editor.Paste);
                }

                break;
            case EditorActionKind.SelectAll:
                editor.SelectAll();
                break;
            case EditorActionKind.Bold:
                WrapSelection(editor, "**", "**", _templates.BoldPlaceholder, runTextMutation);
                break;
            case EditorActionKind.Italic:
                WrapSelection(editor, "*", "*", _templates.ItalicPlaceholder, runTextMutation);
                break;
            case EditorActionKind.InlineCode:
                WrapSelection(editor, "`", "`", _templates.InlineCodePlaceholder, runTextMutation);
                break;
            case EditorActionKind.Link:
                MutateEditor(
                    editor,
                    currentEditor => _textMutationService.InsertLink(
                        currentEditor,
                        _templates.LinkPlaceholder,
                        _templates.LinkUrlPlaceholder),
                    runTextMutation);
                break;
            case EditorActionKind.Image:
                MutateEditor(
                    editor,
                    currentEditor => _textMutationService.InsertImage(
                        currentEditor,
                        _templates.ImageAltPlaceholder,
                        _templates.ImageTargetPlaceholder),
                    runTextMutation);
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
                WrapSelection(editor, "```csharp\n", "\n```", _templates.CodeFencePlaceholder, runTextMutation);
                break;
            case EditorActionKind.Table:
                MutateEditor(
                    editor,
                    currentEditor => _textMutationService.InsertTable(currentEditor, _templates.TableInsertion),
                    runTextMutation);
                break;
            case EditorActionKind.MathBlock:
                WrapSelection(editor, "$$\n", "\n$$", _templates.MathPlaceholder, runTextMutation);
                break;
            case EditorActionKind.HorizontalRule:
                InsertText(editor, "\n---\n", runTextMutation);
                break;
            case EditorActionKind.SmartNewLine:
                MutateEditor(editor, _textMutationService.InsertSmartNewLine, runTextMutation);
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

    private async Task<bool> TryPasteHtmlAsMarkdownAsync(TextEditor editor, Action<Action> runTextMutation)
    {
        try
        {
            var clipboard = TopLevel.GetTopLevel(editor)?.Clipboard;
            if (clipboard is null)
            {
                return false;
            }

            var htmlContent = await TryGetClipboardHtmlAsync(clipboard);
            if (string.IsNullOrWhiteSpace(htmlContent))
            {
                return false;
            }

            var markdown = MarkdownHtmlClipboard.Html2Markdown(htmlContent);
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return false;
            }

            runTextMutation(() => _textMutationService.InsertText(editor, markdown));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<string?> TryGetClipboardHtmlAsync(IClipboard clipboard)
    {
        var html = await clipboard.TryGetValueAsync(MarkdownHtmlClipboard.HtmlMimeFormat);
        if (!string.IsNullOrWhiteSpace(html))
        {
            return html;
        }

        html = await clipboard.TryGetValueAsync(MarkdownHtmlClipboard.MacHtmlFormat);
        if (!string.IsNullOrWhiteSpace(html))
        {
            return html;
        }

        var windowsHtml = await clipboard.TryGetValueAsync(MarkdownHtmlClipboard.WindowsHtmlFormat);
        return windowsHtml is { Length: > 0 }
            ? Encoding.UTF8.GetString(windowsHtml)
            : null;
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
