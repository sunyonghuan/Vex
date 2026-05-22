using CodeWF.EventBus;
using Vex.Core.Messaging;

namespace Vex.Modules.Shell.ViewModels;

public sealed class ShellEditorActionsViewModel
{
    private readonly IEventBus _eventBus;

    public ShellEditorActionsViewModel(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public void Undo()
    {
        Publish(EditorActionKind.Undo);
    }

    public void Redo()
    {
        Publish(EditorActionKind.Redo);
    }

    public void Cut()
    {
        Publish(EditorActionKind.Cut);
    }

    public void Copy()
    {
        Publish(EditorActionKind.Copy);
    }

    public void Paste()
    {
        Publish(EditorActionKind.Paste);
    }

    public void SelectAll()
    {
        Publish(EditorActionKind.SelectAll);
    }

    public void FocusEditor()
    {
        Publish(EditorActionKind.FocusEditor);
    }

    public void InsertAction(EditorActionKind action)
    {
        Publish(action);
    }

    private void Publish(EditorActionKind action)
    {
        _eventBus.Publish(new EditorActionCommand(action));
    }
}
