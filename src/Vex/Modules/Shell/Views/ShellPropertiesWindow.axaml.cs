using Ursa.Controls;

namespace Vex.Modules.Shell.Views;

public partial class ShellPropertiesWindow : UrsaWindow
{
    public ShellPropertiesWindow()
    {
        InitializeComponent();
    }

    public ShellPropertiesWindow(object dataContext)
        : this()
    {
        DataContext = dataContext;
    }
}
