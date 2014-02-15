using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NinePlaces
{

    public partial class DateSelection : UserControl
	{
		public DateSelection()
		{
			// Required to initialize variables
			InitializeComponent();
            

            DTDisplay.GotFocus += new RoutedEventHandler(DTDisplay_GotFocus);

            //KeyDown += new KeyEventHandler(DateSelection_KeyDown);
            DTControls.KeyDown += new KeyEventHandler(DateSelection_KeyDown);

            DTCalendar.IsTodayHighlighted = false;
            DTCalendar.SelectionMode = CalendarSelectionMode.SingleDate;

            DTCalendar.SelectedDatesChanged += new EventHandler<SelectionChangedEventArgs>(DTCalendar_SelectedDatesChanged);

            DTHour.GotFocus += new RoutedEventHandler(TBGotFocus);
            DTHour.LostFocus += new RoutedEventHandler(TBLostFocus);
            DTHour.TextChanged += new TextChangedEventHandler(Hour_TextChanged);
            DTHour.SelectionChanged += new RoutedEventHandler(TBSelectionChanged);

            DTMinute.GotFocus += new RoutedEventHandler(TBGotFocus);
            DTMinute.LostFocus += new RoutedEventHandler(TBLostFocus);
            DTMinute.TextChanged += new TextChangedEventHandler(Minute_TextChanged);
            DTMinute.SelectionChanged += new RoutedEventHandler(TBSelectionChanged);

            DTAMPM.LostFocus += new RoutedEventHandler(TBLostFocus);
            DTAMPM.GotFocus += new RoutedEventHandler(TBGotFocus);
            DTAMPM.TextChanged += new TextChangedEventHandler(AMPM_TextChanged);
            DTAMPM.SelectionChanged += new RoutedEventHandler(TBSelectionChanged);
        }


        void DTCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            DateTime dtNew = (DateTime)DTCalendar.SelectedDate;
            if (InternalDateTime.Hour != dtNew.Hour || InternalDateTime.Minute != dtNew.Minute)
            {
                dtNew = dtNew.AddHours(InternalDateTime.Hour - dtNew.Hour);
                dtNew = dtNew.AddMinutes(InternalDateTime.Minute - dtNew.Minute);
            }

            InternalDateTime = dtNew;
        }

        void DateSelection_KeyDown(object sender, KeyEventArgs e)
        {
            if (DTPopup.IsOpen == true)
            {
                if (e.Key == Key.Escape || e.Key == Key.Enter)
                {
                    focus.Focus();
                    ClosePopup(e.Key == Key.Enter);

                    e.Handled = true;
                }
                if (e.Key == Key.Tab)
                {
                    DTDisplay.Focus();
                    e.Handled = true;
                }
            }
        }

        private DateTime m_dtInternal;

        private DateTime InternalDateTime
        {
            get
            {
                return m_dtInternal;

            }
            set
            {
                if (m_dtInternal != value)
                {
                    m_dtInternal = value;
                }
            }
        }


        public static readonly DependencyProperty SelectedDateTimeProperty = DependencyProperty.Register("SelectedDateTime", typeof(DateTime), typeof(DateSelection), new PropertyMetadata(new PropertyChangedCallback(SelectedDateTimeDateChangedCallback)));
        internal DateTime SelectedDateTime
        {
            get { return (DateTime)this.GetValue(SelectedDateTimeProperty); }
            set { base.SetValue(SelectedDateTimeProperty, value); }
        }
        private static void SelectedDateTimeDateChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DateSelection r = d as DateSelection;
            if (r.DTPopup.IsOpen)
                 r.ClosePopup(false);
            r.DTDisplay.Text = r.SelectedDateTime.ToString("MMMM d, h:mm tt");
        }
 
        void DTDisplay_GotFocus(object sender, RoutedEventArgs e)
        {
            if (DTPopup.IsOpen)
            {
                focus.Focus();      // focus the invisible hidden hack textbox so that we don't reopen the popup.
                ClosePopup(true);
            }
            else
            {
                OpenPopup();
            }
        }

        void PopupLostFocus(object sender, RoutedEventArgs e)
        {
            FrameworkElement oFocused = (FrameworkElement) FocusManager.GetFocusedElement();
            if (oFocused == null ||
                !(
                oFocused == DTAMPM ||
                oFocused == DTMinute ||
                oFocused == DTHour ||
                oFocused == DTCalendar ||
                oFocused == DTDisplay ||
                App.IsAncestor(DTCalendar, oFocused)))
            {

                //
                // determine whether or not the focused element belongs to the CALENDAR control
                //
                ClosePopup(true);
            }
        }

        void OpenPopup()
        {
            focus.IsTabStop = true;
            DTPopup.VerticalOffset = DTDisplay.ActualHeight;

            InternalDateTime = SelectedDateTime;

            UpdateFields();

            DTPopup.IsOpen = true;
            DTCalendar.LostFocus += new RoutedEventHandler(PopupLostFocus);
            DTHour.LostFocus += new RoutedEventHandler(PopupLostFocus);
            DTMinute.LostFocus += new RoutedEventHandler(PopupLostFocus);
            DTAMPM.LostFocus += new RoutedEventHandler(PopupLostFocus);

            DTHour.Focus();
        }

        void ClosePopup( bool bKeepValues )
        {
            DTPopup.IsOpen = false;

            if (bKeepValues)
            {
                SelectedDateTime = InternalDateTime;
                DTDisplay.Text = SelectedDateTime.ToString("MMMM d, h:mm tt");
            }

            DTCalendar.LostFocus -= new RoutedEventHandler(PopupLostFocus);
            DTHour.LostFocus -= new RoutedEventHandler(PopupLostFocus);
            DTMinute.LostFocus -= new RoutedEventHandler(PopupLostFocus);
            DTAMPM.LostFocus -= new RoutedEventHandler(PopupLostFocus);

            focus.IsTabStop = false;
        }

        void AMPM_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DTAMPM.Text.ToLower().Contains("a"))
            {
                DTAMPM.Text = "AM";
                if (InternalDateTime.Hour > 12)
                {
                    InternalDateTime = InternalDateTime.AddHours(-12);
                }
                DTCalendar.Focus();
            }
            else if (DTAMPM.Text.ToLower().Contains("proppathTop"))
            {
                DTAMPM.Text = "PM";
                if (InternalDateTime.Hour < 13)
                {
                    InternalDateTime = InternalDateTime.AddHours(12);
                }
                DTCalendar.Focus();
            }
            else
            {
                DTAMPM.Text = InternalDateTime.ToString("tt");
            }
        }

        void Minute_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                int nMinute = Convert.ToInt32(DTMinute.Text);
                if (nMinute >= 0 && nMinute <= 60)
                {
                    InternalDateTime = InternalDateTime.AddMinutes(nMinute - InternalDateTime.Minute);
                }
                else
                {
                    throw (new Exception());
                }

                if (nMinute > 6)
                {
                    DTMinute.Text = InternalDateTime.ToString("mm");
                    DTAMPM.Focus();
                }
                else
                {
                    DTMinute.SelectionChanged -= new RoutedEventHandler(TBSelectionChanged);
                    DTMinute.Select(1, 0);
                    DTMinute.SelectionChanged += new RoutedEventHandler(TBSelectionChanged);
                }
            }
            catch (Exception)
            {
                DTMinute.Text = InternalDateTime.ToString("mm");
                DTMinute.SelectAll();
            }
        }

        void Hour_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                int nHour = Convert.ToInt32(DTHour.Text);
                if (nHour >= 0 && nHour <= 12)
                {
                    int nPMAdjust = InternalDateTime.Hour > 12 ? 12 : 0;
                    InternalDateTime = InternalDateTime.AddHours(nHour - InternalDateTime.Hour + nPMAdjust);
                }

                if (nHour > 1)
                {
                    DTHour.Text = InternalDateTime.ToString("hh");
                    DTMinute.Focus();
                }
                else
                {
                    DTHour.SelectionChanged -= new RoutedEventHandler(TBSelectionChanged);
                    DTHour.Select(1, 0);
                    DTHour.SelectionChanged += new RoutedEventHandler(TBSelectionChanged);
                }
            }
            catch (Exception)
            {
                DTHour.Text = InternalDateTime.ToString("hh");
                DTHour.SelectAll();
            }
        }

        void TBGotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            tb.SelectAll();
        }

        void TBLostFocus(object sender, RoutedEventArgs e)
        {
            UpdateFields();

            
            TextBox tb = sender as TextBox;
            tb.Select(0, 0);
        }

        private void UpdateFields()
        {
            DTCalendar.SelectedDate = InternalDateTime;

            DTHour.Text = InternalDateTime.ToString("hh");
            DTMinute.Text = InternalDateTime.ToString("mm");
            DTAMPM.Text = InternalDateTime.ToString("tt");
        }

        void TBSelectionChanged(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            tb.SelectionChanged -= new RoutedEventHandler(TBSelectionChanged);
            tb.SelectAll();
            tb.SelectionChanged += new RoutedEventHandler(TBSelectionChanged);
        }
	}
}