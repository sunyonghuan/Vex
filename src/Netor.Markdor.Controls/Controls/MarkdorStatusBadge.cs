using Avalonia;
using Avalonia.Controls.Primitives;

namespace Netor.Markdor.Controls;

public class MarkdorStatusBadge : TemplatedControl
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<MarkdorStatusBadge, string?>(nameof(Label));

    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<MarkdorStatusBadge, string?>(nameof(Value));

    // 强调变体：值与描边使用主题色（对应原型的 Badge.accent，如未保存状态）
    public static readonly StyledProperty<bool> IsAccentProperty =
        AvaloniaProperty.Register<MarkdorStatusBadge, bool>(nameof(IsAccent));

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool IsAccent
    {
        get => GetValue(IsAccentProperty);
        set => SetValue(IsAccentProperty, value);
    }
}
