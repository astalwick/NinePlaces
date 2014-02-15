using System;
using System.Globalization;
using System.Windows.Data;

namespace NinePlaces.Editors
{
    public class EditorDateTimeConverter : IValueConverter
    {

        #region IValueConverter Members

        public object Convert(object value, Type targetType,
                              object parameter,
                              CultureInfo culture)
        {
            if (!(value is DateTime))
                throw new ArgumentException("Date Time required!", "value");

            DateTime dt = (DateTime)value;
            return dt.ToString("MMMM d, h:mm tt");
        }

        public object ConvertBack(object value, Type targetType,
                                  object parameter,
                                  CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
