using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;
using Vex.Modules.Shell.ViewModels;

namespace Vex.Modules.Shell.Views;

public partial class ShellFindBarView : UserControl
{
    private ShellFindBarViewModel? _viewModel;

    public ShellFindBarView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => AttachViewModel(DataContext as ShellFindBarViewModel);
        AttachedToVisualTree += (_, _) =>
        {
            AttachViewModel(DataContext as ShellFindBarViewModel);
            FocusSearchIfVisible();
        };
        DetachedFromVisualTree += (_, _) => AttachViewModel(null);
        AttachViewModel(DataContext as ShellFindBarViewModel);
    }

    private void AttachViewModel(ShellFindBarViewModel? viewModel)
    {
        if (ReferenceEquals(_viewModel, viewModel))
        {
            return;
        }

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = viewModel;

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is ShellFindBarViewModel { IsVisible: true }
            && e.PropertyName is nameof(ShellFindBarViewModel.IsVisible))
        {
            FocusSearchIfVisible();
        }
    }

    private void FocusSearchIfVisible()
    {
        if (_viewModel?.IsVisible != true)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            FindTextBox.Focus();
            FindTextBox.SelectAll();
        });
    }
}
