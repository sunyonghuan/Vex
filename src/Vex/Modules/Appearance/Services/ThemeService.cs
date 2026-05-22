using Avalonia;
using Avalonia.Styling;
using Vex.Core.Models;
using Vex.Core.Services;

namespace Vex.Modules.Appearance.Services;

public sealed class ThemeService : IThemeService
{
    private static readonly ThemeOption[] Themes =
    [
        new("Light", "light", ThemeVariant.Light),
        new("Dark", "dark", ThemeVariant.Dark),
        new("System", "system", ThemeVariant.Default)
    ];

    public IReadOnlyList<ThemeOption> GetThemeOptions() => Themes;

    public void ApplyTheme(ThemeOption theme)
    {
        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = theme.ThemeVariant;
        }
    }
}
