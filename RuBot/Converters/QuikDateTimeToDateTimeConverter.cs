using System;
using System.Globalization;
using System.Windows.Data;
using QuikSharp.DataStructures;

namespace RuBot.Converters
{
    public class QuikDateTimeToDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (DateTime)(QuikDateTime)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (QuikDateTime)(DateTime)value;
        }
    }
}
