    using System.Globalization;

namespace GoatVaultClient.Converters;

public class BoolToGoatButtonColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var enabled = value is true;
        var isDarkMode = Application.Current?.UserAppTheme == AppTheme.Dark;

        if (enabled)
        {
            // Red for "Disable" button
            return isDarkMode
                ? Color.FromArgb("#EF5350") // Lighter red for dark mode
                : Color.FromArgb("#F44336"); // Material red for light mode
        }

        // Green for "Enable" button
        return isDarkMode
            ? Color.FromArgb("#66BB6A") // Lighter green for dark mode
            : Color.FromArgb("#4CAF50"); // Material green for light mode
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}