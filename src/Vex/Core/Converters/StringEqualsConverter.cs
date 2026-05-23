using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Vex.Core.Converters;

public sealed class StringEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return string.Equals(
            value?.ToString(),
            parameter?.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
