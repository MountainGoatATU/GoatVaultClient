using System.Globalization;

namespace GoatVaultClient.Converters;

public class BoolToGoatButtonColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var enabled = value is bool b && b;

        // keep consistent with your BoolToColorConverter (green/red)
        return enabled
            ? Color.FromArgb("#4CAF50")
            : Color.FromArgb("#F44336");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}