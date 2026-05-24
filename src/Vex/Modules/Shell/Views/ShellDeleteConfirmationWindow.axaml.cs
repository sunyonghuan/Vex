using Avalonia.Interactivity;
using Ursa.Controls;

namespace Vex.Modules.Shell.Views;

public partial class ShellDeleteConfirmationWindow : UrsaWindow
{
    public ShellDeleteConfirmationWindow()
    {
        InitializeComponent();
    }

    public ShellDeleteConfirmationWindow(string confirmationText, string warningText, string deletePath)
        : this()
    {
        DataContext = new ShellDeleteConfirmationWindowModel(confirmationText, warningText, deletePath);
    }

    private void Cancel_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void Delete_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }
}

public sealed record ShellDeleteConfirmationWindowModel(
    string ConfirmationText,
    string WarningText,
    string DeletePath);
