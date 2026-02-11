using System.Globalization;

namespace GoatVaultClient.Converters;

public class BoolToGoatButtonColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var enabled = value is true;

        return enabled
            ? Color.FromArgb("#F44336")
            : Color.FromArgb("#4CAF50");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}