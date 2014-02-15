using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Globalization;

namespace NinePlaces.Helpers
{
    public static class DateTimeExtensions
    {
        public static string To_MMMDD(this DateTime in_dtToConvert)
        {
            return in_dtToConvert.ToString(CultureInfo.CurrentCulture.DateTimeFormat.MonthDayPattern.Replace("MMMM", "MMM"));
        }

        public static string To_t(this DateTime in_dtToConvert, bool in_bTwentyFour)
        {
            return in_dtToConvert.ToString(in_bTwentyFour ? "H:mm" : "t", CultureInfo.CurrentCulture.DateTimeFormat);
        }

        public static string To_MMMDD_t(this DateTime in_dtToConvert, bool in_bTwentyFour)
        {
            return in_dtToConvert.To_MMMDD() + ", " + in_dtToConvert.To_t(in_bTwentyFour);
        }

        public static string ToCultureAwareString(this DateTime in_dtToConvert, string in_strPattern)
        {
            return in_dtToConvert.ToString(in_strPattern, CultureInfo.CurrentCulture.DateTimeFormat);
        }

        public static string ToPersistableString(this DateTime in_dtToConvert)
        {
            return in_dtToConvert.ToString("yyyy-MM-ddTHH:mm:ss.ff", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
