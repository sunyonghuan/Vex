using System.Diagnostics;
using Vex.Core.Services;

namespace Vex.Modules.Help.Services;

public sealed class HelpService : IHelpService
{
    public Task OpenWebsiteAsync()
    {
        Open("https://codewf.com");
        return Task.CompletedTask;
    }

    public Task OpenFeedbackAsync()
    {
        Open("https://github.com/dotnet9/Vex/issues");
        return Task.CompletedTask;
    }

    private static void Open(string uri)
    {
        Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
    }
}
