using Avalonia;

namespace Vex.Core.Regions;

public static class RegionTab
{
    public static readonly AttachedProperty<string?> HeaderKeyProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, string?>("HeaderKey", typeof(RegionTab));

    public static string? GetHeaderKey(AvaloniaObject element)
    {
        return element.GetValue(HeaderKeyProperty);
    }

    public static void SetHeaderKey(AvaloniaObject element, string? value)
    {
        element.SetValue(HeaderKeyProperty, value);
    }
}
