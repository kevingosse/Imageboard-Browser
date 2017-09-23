using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

using ImageBoard.Parsers.Common;

namespace ImageBoardBrowser.Converters
{
    public class HistoryToColorConverter : IValueConverter
    {
        public Color NeutralColor { get; set; }

        public Color NewRepliesColor { get; set; }

        public Color VisitedColor { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var history = value as HistoryEntry;

            if (history == null)
            {
                return null;
            }

            if (history.OldRepliesCount == 0 && !history.Visited)
            {
                return this.NeutralColor;
            }

            if (history.OldRepliesCount == history.RepliesCount)
            {
                return this.VisitedColor;
            }

            return this.NewRepliesColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
