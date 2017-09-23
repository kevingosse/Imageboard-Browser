using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ImageBoardBrowser.Converters
{
    public class NullVisibilityConverter : IValueConverter
    {
        public bool CollapseIsEmpty { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            if (this.CollapseIsEmpty && value is string && ((string)value).Length == 0)
            {
                return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
