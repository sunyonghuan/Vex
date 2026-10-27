using Avalonia.Controls;
using Avalonia.Interactivity;
using Netor.Markdor.Modules.Shell.ViewModels;

namespace Netor.Markdor.Modules.Shell.Views;

public partial class ShellAboutOverlayView : UserControl
{
    public ShellAboutOverlayView() => InitializeComponent();
    private void Close_OnClick(object? sender, RoutedEventArgs args) => (DataContext as ShellAboutViewModel)?.Close();
}
