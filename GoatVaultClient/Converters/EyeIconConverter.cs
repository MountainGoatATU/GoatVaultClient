using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UraniumUI.Icons.MaterialSymbols;

namespace GoatVaultClient.Converters
{
    public class EyeIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isVisible)
            {
                return isVisible ? MaterialRounded.Visibility_off : MaterialRounded.Visibility;
            }
            return MaterialRounded.Visibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
