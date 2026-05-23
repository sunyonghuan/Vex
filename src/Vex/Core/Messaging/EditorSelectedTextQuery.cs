using CodeWF.EventBus;

namespace Vex.Core.Messaging;

public sealed class EditorSelectedTextQuery : Query<string>
{
    public override string Result { get; set; } = string.Empty;
}
