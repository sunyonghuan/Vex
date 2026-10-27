namespace Netor.Markdor.Modules.Shell.Services;

public interface IShellStartupArgumentPublisher
{
    void PublishStartupArguments(IEnumerable<string> arguments);
}
