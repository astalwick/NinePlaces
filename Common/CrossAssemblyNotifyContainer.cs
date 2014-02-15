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

namespace Common
{
    public delegate void HourFormatEventHandler(object sender, HourFormatEventArgs args);
    public class HourFormatEventArgs
    {
        public HourFormatEventArgs(bool in_bTwentyFourHour)
        {
            TwentyFourHour = in_bTwentyFourHour;
        }

        public bool TwentyFourHour { get; set; }
    }

    public class CrossAssemblyNotifyContainer
    {
        public static event EventHandler LocalizationChange;
        public static void InvokeLocalizationChange( string in_strNewCulture )
        {
            Log.WriteLine("Language has changed to " + in_strNewCulture, LogEventSeverity.Informational);
            if (LocalizationChange != null)
                LocalizationChange.Invoke(null, new EventArgs());
        }

        public static event HourFormatEventHandler HourFormatChange;
        public static void InvokeHourFormatChange(bool in_bTwentyFourHour)
        {
            Log.WriteLine("Hour format has changed to " + (in_bTwentyFourHour ? " 24hr" : " 12hr"), LogEventSeverity.Informational);
            TwentyFourHourMode = in_bTwentyFourHour;
            if (HourFormatChange != null)
                HourFormatChange.Invoke(null, new HourFormatEventArgs(in_bTwentyFourHour));
        }

        public static bool TwentyFourHourMode = false;
    }
}
