using System.Globalization;

namespace GoatVaultClient.Converters;

public class BoolToEnabledDisabledConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? "Enabled" : "Disabled";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}