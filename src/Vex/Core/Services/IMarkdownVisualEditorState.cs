namespace Vex.Core.Services;

public interface IMarkdownVisualEditorState
{
    bool AllowPreviewEdit { get; }

    event EventHandler? Changed;

    void SetAllowPreviewEdit(bool allowEdit);
}
