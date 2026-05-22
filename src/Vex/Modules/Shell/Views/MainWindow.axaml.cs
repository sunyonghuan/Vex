using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Vex.Modules.Shell.ViewModels;

namespace Vex.Modules.Shell.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        ApplyWindowState(viewModel);
    }

    private void TitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void MinimizeClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void CloseClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is MainWindowViewModel viewModel
            && e.PropertyName is nameof(MainWindowViewModel.IsAlwaysOnTop) or nameof(MainWindowViewModel.IsFullScreen))
        {
            ApplyWindowState(viewModel);
        }
    }

    private void ApplyWindowState(MainWindowViewModel viewModel)
    {
        Topmost = viewModel.IsAlwaysOnTop;
        WindowState = viewModel.IsFullScreen ? WindowState.FullScreen : WindowState.Normal;
    }
}
