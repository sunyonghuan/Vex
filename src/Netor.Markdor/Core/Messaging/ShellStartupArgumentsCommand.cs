using CodeWF.Toolkit.EventBus;

namespace Netor.Markdor.Core.Messaging;

public sealed class ShellStartupArgumentsCommand : Command
{
    public ShellStartupArgumentsCommand(IEnumerable<string> arguments)
    {
        Arguments = arguments.ToArray();
    }

    public IReadOnlyList<string> Arguments { get; }
}
