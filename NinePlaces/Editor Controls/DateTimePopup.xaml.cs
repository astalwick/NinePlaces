using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Threading;

namespace NinePlaces
{
	public partial class DateTimePopup : UserControl
	{
        private DispatcherTimer m_DisplayUpdateTimer = new DispatcherTimer();       // we update the display on a timer, because we may be getting many 'set' notifications from timeline drags.
        private DateTime m_dtSelectedDateTime = DateTime.Now;                       // the selected date.
        public DateTime SelectedDateTime 
        { 
            get
            {
                return m_dtSelectedDateTime;
            }
            set
            {
                if (m_dtSelectedDateTime.Ticks != ((DateTime)value).Ticks)
                {
                    m_dtSelectedDateTime = value;       // set the selected date
                    
                    if (Visibility == Visibility.Visible)           // update the datetime display if we're visible.
                    {
                        m_DisplayUpdateTimer.Stop();
                        m_DisplayUpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
                        m_DisplayUpdateTimer.Tick += new EventHandler(m_DisplayUpdateTimer_Tick);
                        m_DisplayUpdateTimer.Start();
                    }
                }
            }
        }

        public bool IsOpen
        {
            get
            {
                return Visibility == Visibility.Visible;
            }
            set
            {
                if ((bool)value)
                {
                    Visibility = Visibility.Visible;
                }
                else
                {
                    Visibility = Visibility.Collapsed;
                }
            }
        }

		public DateTimePopup()
		{
			// Required to initialize variables
			InitializeComponent();

            System.Diagnostics.Debug.Assert(false); // shouldn't be here.
		}

        public DateTimePopup(DateTime in_dtToDisplay)
        {
            // Required to initialize variables
            InitializeComponent();

            // first, update the date using the given datetime.
            m_dtSelectedDateTime = in_dtToDisplay;
            DTCalendar.DisplayDate = SelectedDateTime;
            DTCalendar.SelectedDate = SelectedDateTime;
            // set some styles on the calendar.
            DTCalendar.IsTodayHighlighted = false;
            DTCalendar.SelectionMode = CalendarSelectionMode.SingleDate;
            // we want to know if the calendar date changes.
            DTCalendar.SelectedDatesChanged += new EventHandler<SelectionChangedEventArgs>(DTCalendar_SelectedDatesChanged);

            // we want to know if things change in the hour/minute/ampm textboxes.
            Hour.GotFocus += new RoutedEventHandler(TBGotFocus);
            Hour.LostFocus += new RoutedEventHandler(TBLostFocus);
            Hour.TextChanged += new TextChangedEventHandler(Hour_TextChanged);
            Hour.SelectionChanged += new RoutedEventHandler(TBSelectionChanged);

            Minute.GotFocus += new RoutedEventHandler(TBGotFocus);
            Minute.LostFocus += new RoutedEventHandler(TBLostFocus);
            Minute.TextChanged += new TextChangedEventHandler(Minute_TextChanged);
            Minute.SelectionChanged += new RoutedEventHandler(TBSelectionChanged);

            AMPM.LostFocus += new RoutedEventHandler(TBLostFocus);
            AMPM.GotFocus += new RoutedEventHandler(TBGotFocus);
            AMPM.TextChanged += new TextChangedEventHandler(AMPM_TextChanged);
            AMPM.SelectionChanged += new RoutedEventHandler(TBSelectionChanged);

            
            // update the display in the hour/minute/ampm textboxes.
            Hour.Text = SelectedDateTime.ToString("hh");
            Minute.Text = SelectedDateTime.ToString("mm");
            AMPM.Text = SelectedDateTime.ToString("tt");
        }

        void TBSelectionChanged(object sender, RoutedEventArgs e)
        {
            // you can't individually select letters/numbers.  it's all or nothing.

            // this could be done more cleanly
            TextBox tb = sender as TextBox;
            tb.SelectionChanged -= new RoutedEventHandler(TBSelectionChanged);
            tb.SelectAll();
            tb.SelectionChanged += new RoutedEventHandler(TBSelectionChanged);
        }

        void TBGotFocus(object sender, RoutedEventArgs e)
        {
            // we got focus, so we select everything so that the user can start typing right away.
            TextBox tb = sender as TextBox;
            tb.SelectAll();
        }

        void TBLostFocus(object sender, RoutedEventArgs e)
        {
            // we've lost focus, so we lose selection.
            TextBox tb = sender as TextBox;
            tb.Select(0, 0);
        }

        void AMPM_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (AMPM.Text.ToLower().Contains("a"))
            {
                // we're setting it to AM.
                AMPM.Text = "AM";
                if (SelectedDateTime.Hour >= 12)
                {
                    m_dtSelectedDateTime = m_dtSelectedDateTime.AddHours(-12);
                }
                DTCalendar.Focus();
            }
            else if (AMPM.Text.ToLower().Contains("p"))
            {
                // we're setting it to PM.
                AMPM.Text = "PM";
                if (SelectedDateTime.Hour < 12)
                {
                    m_dtSelectedDateTime = SelectedDateTime.AddHours(12);
                }
                DTCalendar.Focus();
            }
            else
            {
                AMPM.Text = SelectedDateTime.ToString("tt");
            }
        }

        void Minute_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                int nMinute = Convert.ToInt32(Minute.Text);
                if (nMinute >= 0 && nMinute <= 60)
                {
                    m_dtSelectedDateTime = SelectedDateTime.AddMinutes(nMinute - SelectedDateTime.Minute);
                }
                else
                {
                    throw (new Exception());
                }

                if (nMinute > 5)
                {
                    // there's no possible 2 digit minute value that
                    // starts with a number greater than 5, so we ASSUME
                    // that the user is done, and move to the AMPM textbox.
                    Minute.Text = SelectedDateTime.ToString("mm");
                    AMPM.Focus();
                }
                else
                {
                    // ah, we've entered a minute value that could either be a single
                    // digit minute (eg, 4 = "04") or could be the first number of
                    // a double digit minute (eg, 4 = "44").
                    // make sure that the selection is set so that the user can enter the 
                    // next digit.
                    Minute.SelectionChanged -= new RoutedEventHandler(TBSelectionChanged);
                    Minute.Select(1, 0);
                    Minute.SelectionChanged += new RoutedEventHandler(TBSelectionChanged);
                }
            }
            catch (Exception)
            {
                Minute.Text = SelectedDateTime.ToString("mm");
                Minute.SelectAll();
            }
        }

        void Hour_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                int nHour = Convert.ToInt32( Hour.Text );
                if( nHour >= 0 && nHour <= 12 )
                {
                    int nPMAdjust = SelectedDateTime.Hour > 12 ? 12 : 0;
                    m_dtSelectedDateTime = SelectedDateTime.AddHours(nHour - SelectedDateTime.Hour + nPMAdjust);
                }

                if (nHour > 1)
                {
                    // there's no possible 2 digit hour value that
                    // starts with a number greater than 1 (in 12hr time), so we ASSUME
                    // that the user is done, and move to the minute textbox.
                    Hour.Text = SelectedDateTime.ToString("hh");
                    Minute.Focus();
                }
                else
                {
                    Hour.SelectionChanged -= new RoutedEventHandler(TBSelectionChanged);
                    Hour.Select(1, 0);
                    Hour.SelectionChanged += new RoutedEventHandler(TBSelectionChanged);
                }
            }
            catch( Exception )
            {
                Hour.Text = SelectedDateTime.ToString("hh");
                Hour.SelectAll();
            }
        }
        
        void DTCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            // update the selected DATE.  (NOT the time!)
            m_dtSelectedDateTime = new DateTime(DTCalendar.SelectedDate.GetValueOrDefault().Year,
                                        DTCalendar.SelectedDate.GetValueOrDefault().Month,
                                        DTCalendar.SelectedDate.GetValueOrDefault().Day,
                                        SelectedDateTime.Hour,
                                        SelectedDateTime.Minute,
                                        00);

        }

        void m_DisplayUpdateTimer_Tick(object sender, EventArgs e)
        {
            // we only tick once, and then we unregister ourselves.
            m_DisplayUpdateTimer.Stop();
            m_DisplayUpdateTimer.Tick -= new EventHandler(m_DisplayUpdateTimer_Tick);

            // update the textboxes to reflect the currently selected time
            Hour.Text = SelectedDateTime.ToString("hh");
            Minute.Text = SelectedDateTime.ToString("mm");
            AMPM.Text = SelectedDateTime.ToString("tt");
        }
    }
}
