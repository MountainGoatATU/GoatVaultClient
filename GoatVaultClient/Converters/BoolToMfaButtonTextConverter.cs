using System.Globalization;

namespace GoatVaultClient.Converters;

public class BoolToMfaButtonTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isEnabled)
            return isEnabled ? "Disable MFA" : "Enable MFA";

        return "Enable MFA";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}