using System.Globalization;

namespace GoatVaultClient.Converters;

public class TimeToProgressConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int secondsRemaining)
            return 0.0;

        // TOTP codes refresh every 30 seconds
        const int totalSeconds = 30;
        return (double)secondsRemaining / totalSeconds;

    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}