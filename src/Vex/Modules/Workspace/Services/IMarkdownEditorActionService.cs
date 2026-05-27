using AvaloniaEdit;
using Vex.Core.Messaging;

namespace Vex.Modules.Workspace.Services;

public interface IMarkdownEditorActionService
{
    Task ExecuteAsync(TextEditor editor, EditorActionKind action, Action<Action> runTextMutation);
}
