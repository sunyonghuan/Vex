using CodeWF.EventBus;
using Vex.Core.Messaging;

namespace Vex.Modules.Shell.Services;

public sealed class ShellStatusPublisher : IShellStatusPublisher
{
    private readonly IEventBus _eventBus;

    public ShellStatusPublisher(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public void Publish(string message)
    {
        _eventBus.Publish(new WorkspaceStatusChangedCommand(message));
    }
}
