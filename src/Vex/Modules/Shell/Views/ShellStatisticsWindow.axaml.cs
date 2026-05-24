using Ursa.Controls;

namespace Vex.Modules.Shell.Views;

public partial class ShellStatisticsWindow : UrsaWindow
{
    public ShellStatisticsWindow()
    {
        InitializeComponent();
    }

    public ShellStatisticsWindow(object dataContext)
        : this()
    {
        DataContext = dataContext;
    }
}
