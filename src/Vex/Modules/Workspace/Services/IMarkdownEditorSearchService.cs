using AvaloniaEdit;
using Vex.Core.Messaging;

namespace Vex.Modules.Workspace.Services;

public interface IMarkdownEditorSearchService
{
    void Search(
        TextEditor editor,
        EditorSearchCommand command,
        Action<Action> runTextMutation,
        Action publishTextChanged);
}
