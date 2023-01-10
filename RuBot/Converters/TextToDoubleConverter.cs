using System;
using System.Globalization;
using System.Windows.Data;

namespace RuBot.Converters
{
    public class TextToDoubleConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
                              CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                                  CultureInfo culture)
        {
            try
            {
                return double.Parse((string) value);
            }
            catch (Exception)
            {
            }
            return 0;
        }

        #endregion
    }
}