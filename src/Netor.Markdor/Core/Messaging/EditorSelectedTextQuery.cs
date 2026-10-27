using CodeWF.Toolkit.EventBus;

namespace Netor.Markdor.Core.Messaging;

public sealed class EditorSelectedTextQuery : Query<string>
{
    public override string Result { get; set; } = string.Empty;
}
