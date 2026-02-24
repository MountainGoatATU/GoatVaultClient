    using System.Globalization;

namespace GoatVaultClient.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue)
        {
            // Fallback color
            return Application.Current?.UserAppTheme == AppTheme.Dark
                ? Color.FromArgb("#BDBDBD") // Light gray for dark mode
                : Color.FromArgb("#757575"); // Dark gray for light mode
        }

        var isDarkMode = Application.Current?.UserAppTheme == AppTheme.Dark;

        if (boolValue)
        {
            // Green for enabled/valid
            return isDarkMode
                ? Color.FromArgb("#66BB6A") // Lighter green for dark mode
                : Color.FromArgb("#4CAF50"); // Material green for light mode
        }

        // Red for disabled/invalid
        return isDarkMode
            ? Color.FromArgb("#EF5350") // Lighter red for dark mode
            : Color.FromArgb("#F44336"); // Material red for light mode
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}