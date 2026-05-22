using ReactiveUI;

namespace Vex.Modules.Shell.ViewModels;

public sealed class ShellStatusBarViewModel : ReactiveObject
{
    public ShellStatusBarViewModel(
        ShellStatusViewModel status,
        ShellDocumentInfoViewModel documentInfo,
        ShellEditorDisplayViewModel editorDisplay,
        ShellWindowLayoutViewModel layout)
    {
        Status = status;
        DocumentInfo = documentInfo;
        EditorDisplay = editorDisplay;
        Layout = layout;
    }

    public ShellStatusViewModel Status { get; }

    public ShellDocumentInfoViewModel DocumentInfo { get; }

    public ShellEditorDisplayViewModel EditorDisplay { get; }

    public ShellWindowLayoutViewModel Layout { get; }
}
