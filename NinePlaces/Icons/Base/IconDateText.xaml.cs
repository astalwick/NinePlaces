using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using NinePlaces.Helpers;
using System.Windows.Media;

namespace NinePlaces
{
    public class HoverDateTimeConverter : IValueConverter
    {

        #region IValueConverter Members

        public object Convert(object value, Type targetType,
                              object parameter,
                              CultureInfo culture)
        {
            if( !(value is DateTime) )
                throw new ArgumentException("Date Time required!", "value");

            DateTime dt = (DateTime) value;

            return dt.To_MMMDD_t(App.TwentyFourHourMode);
        }

        public object ConvertBack(object value, Type targetType,
                                  object parameter,
                                  CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    // this is the control that hovers above the child when you select it.
	public partial class IconHoverText : UserControl
	{
        private string m_strMajorString;
        public string MajorString
        {
            get
            {
                return m_strMajorString;
            }
            set
            {
                m_strMajorString = value;
            }
        }
        private string m_strMinorString;
        public string MinorString
        {
            get
            {
                return m_strMinorString;
            }
            set
            {
                m_strMinorString = value;
            }
        }

        private DateTime m_dtTime;
        public DateTime DateTimeValue
        {
            set
            {
                m_dtTime = value;

                //MinorString = m_dtTime.To_MMMDD_t(App.TwentyFourHourMode);
            }
        }

        public Brush BackgroundBrush
        {
            get
            {
                return LayoutRoot.Background;
            }
            set
            {
                LayoutRoot.Background = value;
            }
        }

		public IconHoverText()
		{
			// Required to initialize variables
            m_dtTime = DateTime.Now;
			InitializeComponent();
            

            Common.CrossAssemblyNotifyContainer.HourFormatChange += new Common.HourFormatEventHandler(CrossAssemblyNotifyContainer_HourFormatChange);
		}

        void CrossAssemblyNotifyContainer_HourFormatChange(object sender, Common.HourFormatEventArgs args)
        {
            //MinorString = m_dtTime.To_MMMDD_t(App.TwentyFourHourMode);
            object d = DataContext;
            DataContext = null;
            DataContext = d;
        }
	}
}