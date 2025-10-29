using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MobileLinesManager.Converters
{
    public class HexToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hexColor && !string.IsNullOrWhiteSpace(hexColor))
            {
                try
                {
                    if (!hexColor.StartsWith("#"))
                        hexColor = "#" + hexColor;

                    return (SolidColorBrush)(new BrushConverter().ConvertFromString(hexColor));
                }
                catch
                {
                    return Brushes.Gray;
                }
            }

            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
