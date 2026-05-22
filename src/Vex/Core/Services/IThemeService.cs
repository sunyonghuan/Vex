using Vex.Core.Models;

namespace Vex.Core.Services;

public interface IThemeService
{
    IReadOnlyList<ThemeOption> GetThemeOptions();

    void ApplyTheme(ThemeOption theme);
}
