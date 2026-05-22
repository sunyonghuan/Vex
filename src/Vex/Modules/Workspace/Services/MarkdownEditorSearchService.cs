using AvaloniaEdit;
using CodeWF.EventBus;
using Vex.Core.Messaging;
using Vex.Core.Services;

namespace Vex.Modules.Workspace.Services;

public sealed class MarkdownEditorSearchService : IMarkdownEditorSearchService
{
    private readonly IEventBus _eventBus;
    private readonly IAppLocalizer _localizer;

    public MarkdownEditorSearchService(IEventBus eventBus, IAppLocalizer localizer)
    {
        _eventBus = eventBus;
        _localizer = localizer;
    }

    public void Search(
        TextEditor editor,
        EditorSearchCommand command,
        Action<Action> runTextMutation,
        Action publishTextChanged)
    {
        if (string.IsNullOrEmpty(command.SearchText))
        {
            PublishSearchResult(VexL.StatusEnterSearchTextFirst);
            return;
        }

        switch (command.Action)
        {
            case EditorSearchAction.Count:
                CountMatches(editor, command.SearchText);
                break;
            case EditorSearchAction.FindNext:
                FindNext(editor, command.SearchText, editor.CaretOffset + Math.Max(0, editor.SelectionLength), publishTextChanged);
                break;
            case EditorSearchAction.ReplaceNext:
                ReplaceNext(editor, command.SearchText, command.ReplacementText, runTextMutation, publishTextChanged);
                break;
            case EditorSearchAction.ReplaceAll:
                ReplaceAll(editor, command.SearchText, command.ReplacementText, runTextMutation);
                break;
        }
    }

    private void FindNext(TextEditor editor, string searchText, int startOffset, Action publishTextChanged)
    {
        if (editor.Document is null)
        {
            return;
        }

        var text = editor.Text ?? string.Empty;
        var matches = FindMatches(text, searchText);
        var matchIndex = GetNextMatchIndex(matches, startOffset);
        var index = matchIndex >= 0 ? matches[matchIndex] : -1;
        if (index < 0)
        {
            PublishSearchResultFormat(VexL.EditorSearchNoMatchFormat, searchText);
            return;
        }

        editor.Select(index, searchText.Length);
        editor.CaretOffset = index + searchText.Length;
        editor.TextArea.Caret.BringCaretToView();
        editor.Focus();
        var line = editor.Document.GetLineByOffset(index).LineNumber;
        _eventBus.Publish(new EditorSearchResultCommand(
            _localizer.Format(VexL.EditorSearchFoundOnLineFormat, searchText, line, matchIndex + 1, matches.Count),
            matchIndex + 1,
            matches.Count));
        publishTextChanged();
    }

    private void ReplaceNext(
        TextEditor editor,
        string searchText,
        string replacementText,
        Action<Action> runTextMutation,
        Action publishTextChanged)
    {
        var text = editor.Text ?? string.Empty;
        var start = Math.Clamp(editor.SelectionStart, 0, text.Length);
        var length = Math.Clamp(editor.SelectionLength, 0, text.Length - start);
        var selected = length > 0 ? text.Substring(start, length) : string.Empty;

        if (!selected.Equals(searchText, StringComparison.CurrentCultureIgnoreCase))
        {
            FindNext(editor, searchText, editor.CaretOffset, publishTextChanged);
            return;
        }

        runTextMutation(() =>
        {
            editor.Text = text[..start] + replacementText + text[(start + length)..];
            editor.CaretOffset = start + replacementText.Length;
            editor.Select(start, replacementText.Length);
        });
        PublishSearchResultFormat(VexL.EditorSearchReplacedNextFormat, searchText);
        FindNext(editor, searchText, start + replacementText.Length, publishTextChanged);
    }

    private void ReplaceAll(
        TextEditor editor,
        string searchText,
        string replacementText,
        Action<Action> runTextMutation)
    {
        var text = editor.Text ?? string.Empty;
        var comparison = StringComparison.CurrentCultureIgnoreCase;
        var builder = new System.Text.StringBuilder(text.Length);
        var offset = 0;
        var count = 0;

        while (offset < text.Length)
        {
            var index = text.IndexOf(searchText, offset, comparison);
            if (index < 0)
            {
                builder.Append(text, offset, text.Length - offset);
                break;
            }

            builder.Append(text, offset, index - offset);
            builder.Append(replacementText);
            offset = index + searchText.Length;
            count++;
        }

        if (count == 0)
        {
            PublishSearchResultFormat(VexL.EditorSearchNoMatchFormat, searchText);
            return;
        }

        runTextMutation(() =>
        {
            editor.Text = builder.ToString();
            editor.CaretOffset = 0;
            editor.SelectionLength = 0;
        });
        PublishSearchResultFormat(VexL.EditorSearchReplacedAllFormat, count);
    }

    private void CountMatches(TextEditor editor, string searchText)
    {
        var matches = FindMatches(editor.Text ?? string.Empty, searchText);
        if (matches.Count == 0)
        {
            PublishSearchResultFormat(VexL.EditorSearchNoMatchFormat, searchText);
            return;
        }

        var matchIndex = GetNextMatchIndex(matches, editor.CaretOffset);
        _eventBus.Publish(new EditorSearchResultCommand(
            _localizer.Format(VexL.EditorSearchMatchCountFormat, matches.Count, searchText),
            matchIndex >= 0 ? matchIndex + 1 : 1,
            matches.Count));
    }

    private void PublishSearchResult(string key)
    {
        _eventBus.Publish(new EditorSearchResultCommand(_localizer.Get(key)));
    }

    private void PublishSearchResultFormat(string key, params object?[] args)
    {
        _eventBus.Publish(new EditorSearchResultCommand(_localizer.Format(key, args)));
    }

    private static List<int> FindMatches(string text, string searchText)
    {
        if (text.Length == 0 || searchText.Length == 0)
        {
            return [];
        }

        List<int> matches = [];
        var comparison = StringComparison.CurrentCultureIgnoreCase;
        var offset = 0;
        while (offset <= text.Length - searchText.Length)
        {
            var index = text.IndexOf(searchText, offset, comparison);
            if (index < 0)
            {
                break;
            }

            matches.Add(index);
            offset = index + Math.Max(1, searchText.Length);
        }

        return matches;
    }

    private static int GetNextMatchIndex(IReadOnlyList<int> matches, int startOffset)
    {
        if (matches.Count == 0)
        {
            return -1;
        }

        var start = Math.Max(0, startOffset);
        for (var i = 0; i < matches.Count; i++)
        {
            if (matches[i] >= start)
            {
                return i;
            }
        }

        return 0;
    }
}
