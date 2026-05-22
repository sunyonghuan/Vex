using Vex.Core.Services;

namespace Vex.Modules.Workspace.Services;

public sealed class MarkdownVisualEditorState : IMarkdownVisualEditorState
{
    private bool _allowPreviewEdit = true;

    public bool AllowPreviewEdit => _allowPreviewEdit;

    public event EventHandler? Changed;

    public void SetAllowPreviewEdit(bool allowEdit)
    {
        if (_allowPreviewEdit == allowEdit)
        {
            return;
        }

        _allowPreviewEdit = allowEdit;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
