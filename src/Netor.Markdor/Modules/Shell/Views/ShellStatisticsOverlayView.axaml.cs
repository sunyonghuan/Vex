using Avalonia.Controls;
using Avalonia.Interactivity;
using Netor.Markdor.Modules.Shell.Services;

namespace Netor.Markdor.Modules.Shell.Views;

public partial class ShellStatisticsOverlayView : UserControl
{
    public ShellStatisticsOverlayView()
    {
        InitializeComponent();
    }

    private void Close_OnClick(object? sender, RoutedEventArgs e) => ShellOverlayHost.Dismiss(this);
}
