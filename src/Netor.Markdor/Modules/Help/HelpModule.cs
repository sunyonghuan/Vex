using Microsoft.Extensions.DependencyInjection;
using Netor.Markdor.Core.Modularity;

namespace Netor.Markdor.Modules.Help;

public sealed class HelpModule : IAppModule
{
    public void RegisterServices(IServiceCollection services)
    {
        // 目前为空，保留用于未来扩展
    }

    public void Initialize(IServiceProvider serviceProvider)
    {
        // 目前为空，保留用于未来扩展
    }
}
