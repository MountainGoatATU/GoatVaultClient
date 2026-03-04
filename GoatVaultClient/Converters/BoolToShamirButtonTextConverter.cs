using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GoatVaultClient.Converters;

public class BoolToShamirButtonTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isEnabled)
            return isEnabled ? "Disable Shamir Backup" : "Enable Shamir Backup";

        return "Enable Shamir Backup";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
