using System.Globalization;

namespace GoatVaultClient.Converters;

public class BoolToEnableDisableConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? "Disable" : "Enable";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}