using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ImageBoardBrowser.Converters
{
    public class StringToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var source = value as string;

            if (source == null)
            {
                return null;
            }

            var seed = source.GetHashCode();

            //return new SolidColorBrush(new Color { A = 30 }.GetRandom(0.8, 1, seed));
            return new Color { A = 60 }.GetRandom(0.8, 1, seed);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
