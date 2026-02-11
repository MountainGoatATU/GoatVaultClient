using System.Globalization;

namespace GoatVaultClient.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue
                ? Color.FromArgb("#4CAF50") // Green for enabled/valid
                : Color.FromArgb("#F44336"); // Red for disabled/invalid
        }

        return Colors.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}