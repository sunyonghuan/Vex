using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Ursa.Controls;
using Vex.Modules.Shell.Services;
using Vex.Modules.Shell.ViewModels;

namespace Vex.Modules.Shell.Views;

public partial class MainWindow : UrsaWindow
{
    private ShellKeyboardShortcutViewModel? _keyboardShortcuts;
    private bool _isCloseConfirmed;

    public MainWindow()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, WindowKeyDown, RoutingStrategies.Tunnel);
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, WindowDragOver);
        AddHandler(DragDrop.DropEvent, WindowDrop);
    }

    public MainWindow(
        MainWindowViewModel viewModel,
        ShellActionCoordinator actionCoordinator,
        ShellKeyboardShortcutViewModel keyboardShortcuts)
        : this()
    {
        // 强制解析 ShellActionCoordinator，让标题栏菜单的 EventBus 动作路由在窗口创建时完成订阅。
        _ = actionCoordinator;
        _keyboardShortcuts = keyboardShortcuts;
        DataContext = viewModel;
        viewModel.Layout.PropertyChanged += OnLayoutPropertyChanged;
        viewModel.CloseWindowRequested += OnCloseWindowRequested;
        ApplyWindowState(viewModel.Layout);
        Opened += async (_, _) => await viewModel.OpenStartupDocumentAsync(Environment.GetCommandLineArgs().Skip(1));
    }

    private void WindowKeyDown(object? sender, KeyEventArgs e)
    {
        e.Handled = _keyboardShortcuts?.HandleKeyDown(e.Key, e.KeyModifiers) == true;
    }

    private void OnLayoutPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is ShellWindowLayoutViewModel layout
            && e.PropertyName is nameof(ShellWindowLayoutViewModel.IsAlwaysOnTop) or nameof(ShellWindowLayoutViewModel.IsFullScreen))
        {
            ApplyWindowState(layout);
        }
    }

    private void ApplyWindowState(ShellWindowLayoutViewModel layout)
    {
        Topmost = layout.IsAlwaysOnTop;
        WindowState = layout.IsFullScreen ? WindowState.FullScreen : WindowState.Normal;
    }

    protected override async Task<bool> CanClose()
    {
        if (_isCloseConfirmed)
        {
            return true;
        }

        if (DataContext is MainWindowViewModel viewModel && viewModel.DocumentInfo.IsModified)
        {
            await viewModel.BeginWindowCloseAsync();
            return false;
        }

        return true;
    }

    private void OnCloseWindowRequested(object? sender, EventArgs e)
    {
        _isCloseConfirmed = true;
        Close();
    }

    private void WindowDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = GetFirstDroppedLocalPath(e) is null
            ? DragDropEffects.None
            : DragDropEffects.Copy;
        e.Handled = true;
    }

    private async void WindowDrop(object? sender, DragEventArgs e)
    {
        e.Handled = true;
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var path = GetFirstDroppedLocalPath(e);
        if (path is not null)
        {
            await viewModel.OpenDroppedPathAsync(path);
        }
    }

    private static string? GetFirstDroppedLocalPath(DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles();
        if (files is null)
        {
            return null;
        }

        foreach (var item in files)
        {
            var path = item.TryGetLocalPath();
            if (!string.IsNullOrWhiteSpace(path) && (File.Exists(path) || Directory.Exists(path)))
            {
                return path;
            }
        }

        return null;
    }
}
