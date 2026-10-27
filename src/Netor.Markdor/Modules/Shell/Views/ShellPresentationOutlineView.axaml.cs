using Avalonia.Controls;
using Avalonia.Interactivity;
using Netor.Markdor.Modules.Shell.ViewModels;

namespace Netor.Markdor.Modules.Shell.Views;

public partial class ShellPresentationOutlineView : UserControl
{
    public ShellPresentationOutlineView() => InitializeComponent();
    private void ChapterClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int index } && DataContext is MainWindowViewModel vm)
            vm.Workbench.NavigatePresentation(index);
    }
}
