using Avalonia.Controls;
using System.Linq;
using Netor.Markdor.Controls;

namespace Netor.Markdor.Modules.Shell.Views;

public partial class ShellEditorOptionsView : UserControl
{
    public ShellEditorOptionsView()
    {
        InitializeComponent();
        if (FormatHeading.Flyout is MenuFlyout headingMenu)
        {
            foreach (var item in headingMenu.Items.OfType<MenuItem>()) MenuIconCatalog.Apply(item);
        }
    }
}
