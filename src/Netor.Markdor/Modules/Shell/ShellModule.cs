using Microsoft.Extensions.DependencyInjection;
using Netor.Markdor.Core.Modularity;
using Netor.Markdor.Core.Regions;
using Netor.Markdor.Modules.Shell.Views;

namespace Netor.Markdor.Modules.Shell;

public sealed class ShellModule : IAppModule
{
    public void RegisterServices(IServiceCollection services)
    {
        // 目前为空，保留用于未来扩展
    }

    public void Initialize(IServiceProvider serviceProvider)
    {
        var regionManager = serviceProvider.GetRequiredService<IRegionManager>();
        regionManager.RegisterViewWithRegion<ShellFilesView>(RegionNames.ShellSidebarRegion);
        regionManager.RegisterViewWithRegion<ShellOutlineView>(RegionNames.ShellOutlineRegion);
    }
}
