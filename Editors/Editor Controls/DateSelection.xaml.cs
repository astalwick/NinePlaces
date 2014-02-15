using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Globalization;
using System.Windows.Media;
using Common;

namespace BasicEditors
{

    public partial class DateSelection : UserControl
	{
        private bool m_bChangeMade = false;
		public DateSelection()
		{
			// Required to initialize variables
			InitializeComponent();

            DTDisplay.GotFocus += new RoutedEventHandler(DTDisplay_GotFocus);
            DTControls.KeyDown += new KeyEventHandler(DateSelection_KeyDown);

            //
            // set up the calendar events and initial state.
            //
            DTCalendar.IsTodayHighlighted = false;
            DTCalendar.SelectionMode = CalendarSelectionMode.SingleDate;
            DTCalendar.SelectedDatesChanged += new EventHandler<SelectionChangedEventArgs>(DTCalendar_SelectedDatesChanged);

            //
            // set up the events for the text fields.
            //
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

            CrossAssemblyNotifyContainer.HourFormatChange += new HourFormatEventHandler(CrossAssemblyNotifyContainer_HourFormatChange);

            UpdateHourFormat();
        }

        private void UpdateHourFormat()
        {
            DTDisplay.Text = SelectedDateTime.ToString("MMMM d, " + (CrossAssemblyNotifyContainer.TwentyFourHourMode ? "H:mm" : "h:mm tt"), CultureInfo.CurrentCulture.DateTimeFormat);
            DTAMPM.Visibility = CrossAssemblyNotifyContainer.TwentyFourHourMode ? Visibility.Collapsed : Visibility.Visible;

            DTHour.Text = InternalDateTime.ToString(CrossAssemblyNotifyContainer.TwentyFourHourMode ? "HH" : "hh");
        }

        private void CrossAssemblyNotifyContainer_HourFormatChange(object sender, HourFormatEventArgs args)
        {
            UpdateHourFormat();
        }

        void DTCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            //
            // the date changed.  
            // we'll create a new DateTime object that
            // has the new DATE value.  we need to copy over
            // the hour and minute, though!  otherwise we'll
            // end up with a DateTime object pointing to 12am.
            //
            m_bChangeMade = true;
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
                    //
                    // ok, we've hit escape or enter.  that means that we're
                    // going to be closing the popup.  focus the 'focus' text
                    // box so that any textboxes that had been edited get
                    // their values pushed through the binding mechanism
                    // into their property.
                    //
                    // (we have to set istabstop so that the 'focus' obejct
                    // is temporarily allowed to receive focus).
                    //
                    focus.IsTabStop = true;
                    focus.Focus();
                    focus.IsTabStop = false;

                    // close the popup - if we hit enter, persist the values.
                    ClosePopup(e.Key == Key.Enter);

                    e.Handled = true;
                }
                if (e.Key == Key.Tab && !m_bChangeMade )
                {
                    //
                    // we haven't made any edits, so a tab key-press
                    // pushes us through the remaining edit fields of
                    // our owner control.  that means that *this* 
                    // tab keypress closes our calendar.
                    //

                    // DTDisplay is our edit field that sits in the 
                    // main editor.  it is outside of the popup.  we
                    // focus it so that the next tab press moves on
                    // in the main editor.
                    DTDisplay.Focus();
                    e.Handled = true;
                }
            }
        }

        private DateTime m_dtInternal;

        /// <summary>
        /// The DateTime including any edits (even before the edits
        /// have been persisted into SelectedDateTime).
        /// </summary>
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
        public DateTime SelectedDateTime
        {
            get { return (DateTime)this.GetValue(SelectedDateTimeProperty); }
            set { base.SetValue(SelectedDateTimeProperty, value); }
        }

        private static void SelectedDateTimeDateChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //
            // this gets called when someone outside the edit control
            // changes the date.  force the calendar popup to close when that
            // happens, and update the text field's displaytext.
            //
            DateSelection r = d as DateSelection;
            if (r.DTPopup.IsOpen)
                 r.ClosePopup(false);
            r.DTDisplay.Text = r.SelectedDateTime.ToString("MMMM d, " + (CrossAssemblyNotifyContainer.TwentyFourHourMode ? "H:mm" : "h:mm tt"), CultureInfo.CurrentCulture.DateTimeFormat);
        }
 
        void DTDisplay_GotFocus(object sender, RoutedEventArgs e)
        {

            if (DTPopup.IsOpen)
            {
                //
                // if the popup is open and we've clicked on the display (the 
                // edit field in the main editor), then  we should close the
                // popup.
                //
                focus.IsTabStop = true;
                focus.Focus();      // focus the invisible hidden hack textbox so that we don't reopen the popup.
                focus.IsTabStop = false;
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
                Helpers.IsAncestorControl(DTCalendar, oFocused)))          // the calendar control has a whole host of 
                                                            // subcontrols that can take focus.  we shouldn't
                                                            // close the popup if they get focus.
            {

                //
                // determine whether or not the focused element belongs to the CALENDAR control
                //
                ClosePopup(true);
            }
        }

        private bool KeepOnScreen(UIElement in_oElementToCheck, double in_dTop, double in_dLeft, double in_dDesiredHeight, ref double out_nFixOffsetY)
        {
            // get the timeline element.  we need this to tell exactly where our bounds are.
            FrameworkElement oTimeline = (Helpers.App.PageRoot as FrameworkElement).FindName("Timeline") as FrameworkElement;
            GeneralTransform aobjGeneralTransform = in_oElementToCheck.TransformToVisual(oTimeline);
            Point pt = aobjGeneralTransform.Transform(new Point(0, 0));

            out_nFixOffsetY = 0;
            if (pt.Y < 0)
            {
                out_nFixOffsetY = pt.Y;
            }

            if (pt.Y + in_dDesiredHeight > oTimeline.ActualHeight)
            {
                out_nFixOffsetY = -1.0 * (pt.Y + in_dDesiredHeight - oTimeline.ActualHeight);
            }

            return true;
        }

        void OpenPopup()
        {
            // push the popup so that it opens *below* the display edit control
            double dTop = Canvas.GetTop(this);
            double dLeft = Canvas.GetLeft(this);
        
            DTPopup.IsOpen = true;
            DTControls.UpdateLayout();//

            double nFixY = 0;
            KeepOnScreen(DTPopup.Parent as UIElement, dTop, dLeft, DTControls.DesiredSize.Height + DTDisplay.ActualHeight, ref nFixY);

            if (nFixY != 0)
                DTPopup.VerticalOffset = nFixY;
            else
                DTPopup.VerticalOffset = DTDisplay.ActualHeight;

            // initialize the internal time to match the currently persisted time.
            InternalDateTime = SelectedDateTime;

            // update the calendar and the edit controls.
            UpdateFields();

            // set up the events.
            DTCalendar.LostFocus += new RoutedEventHandler(PopupLostFocus);
            DTHour.LostFocus += new RoutedEventHandler(PopupLostFocus);
            DTMinute.LostFocus += new RoutedEventHandler(PopupLostFocus);
            DTAMPM.LostFocus += new RoutedEventHandler(PopupLostFocus);

            // we start with the hour focused, so that you can click and start typing right away.
            DTHour.Focus();
            m_bChangeMade = false;
        }

        void ClosePopup( bool bKeepValues )
        {
            DTPopup.IsOpen = false;

            if (bKeepValues)
            {
                // persist out the edited date time.
                SelectedDateTime = InternalDateTime;
                DTDisplay.Text = SelectedDateTime.ToString("MMMM d, " + (CrossAssemblyNotifyContainer.TwentyFourHourMode ? "H:mm" : "h:mm tt") , CultureInfo.CurrentCulture.DateTimeFormat);
            }

            // the popup is gone, so remove the events associated with it.
            DTCalendar.LostFocus -= new RoutedEventHandler(PopupLostFocus);
            DTHour.LostFocus -= new RoutedEventHandler(PopupLostFocus);
            DTMinute.LostFocus -= new RoutedEventHandler(PopupLostFocus);
            DTAMPM.LostFocus -= new RoutedEventHandler(PopupLostFocus);
        }

        /// <summary>
        /// updates the AM/PM text field based on what the user
        /// has entered
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AMPM_TextChanged(object sender, TextChangedEventArgs e)
        {
            m_bChangeMade = true;
            if (DTAMPM.Text.ToLower().Contains("a"))
            {
                // if the user hits the 'a' key in the ampm field, 
                // then we change the AM.
                DTAMPM.Text = "AM";
                if (InternalDateTime.Hour > 11)
                {
                    InternalDateTime = InternalDateTime.AddHours(-12);
                }
                DTCalendar.Focus();
            }
            else if (DTAMPM.Text.ToLower().Contains("p"))
            {
                // if the user hits the 'p' key in the ampm field, 
                // then we change the PM.
                DTAMPM.Text = "PM";
                if (InternalDateTime.Hour < 12)
                {
                    InternalDateTime = InternalDateTime.AddHours(12);
                }
                DTCalendar.Focus();
            }
            else
            {
                // nothing really to do.  lets just make sure we're up to date.
                DTAMPM.Text = InternalDateTime.ToString("tt");
            }
        }

        void Minute_TextChanged(object sender, TextChangedEventArgs e)
        {
            m_bChangeMade = true;
            try
            {
                int nMinute = Convert.ToInt32(DTMinute.Text);
                if (nMinute >= 0 && nMinute < 60)
                {
                    // update the internaldate to the right time.
                    // if we typed in '6' and the minute value is currently
                    // 54,
                    // then we just need to adjust the internaldatetime back
                    // 48 minutes.
                    InternalDateTime = InternalDateTime.AddMinutes(nMinute - InternalDateTime.Minute);
                }
                else
                {
                    throw (new Exception());
                }

                if (nMinute > 5 || DTMinute.Text=="00")
                {
                    // ok, the current value of the edit field is greater than
                    // 6.  there's no way that another keypress is necessary in 
                    // this field.  update the text field and move on to the
                    // ampm field.
                    DTMinute.Text = InternalDateTime.ToString("mm");
                    DTAMPM.Focus();
                }
                else
                {
                    // it's possible that the user has entered the value '5', and
                    // wants to enter '4' - to get to '54'.  so, lets move the
                    // minute edit field caret to the right position, and listen
                    // for more keypresses on thsi field.
                    DTMinute.SelectionChanged -= new RoutedEventHandler(TBSelectionChanged);
                    DTMinute.Select(1, 0);
                    DTMinute.SelectionChanged += new RoutedEventHandler(TBSelectionChanged);
                }
            }
            catch (Exception)
            {
                FrameworkElement fw = (FrameworkElement)FocusManager.GetFocusedElement();
                if (fw == DTMinute && DTMinute.Text == "")
                {
                    // fine.  the user hit backspace or something.
                    // we'll allow this error to ride.
                    return;
                }
                DTMinute.Text = InternalDateTime.ToString("mm");
                DTMinute.SelectAll();
            }
        }

        void Hour_TextChanged(object sender, TextChangedEventArgs e)
        {
            m_bChangeMade = true;
            try
            {
                // see comments for Minute_TextChhanged - same basic idea.
                int nHour = Convert.ToInt32(DTHour.Text);

                if (!CrossAssemblyNotifyContainer.TwentyFourHourMode)
                {
                    bool bAM = InternalDateTime.Hour < 12;
                    if (nHour >= 0 && nHour <= 12)
                    {
                        int nPMAdjust = InternalDateTime.Hour > 11 ? 12 : 0;

                        if (nHour == 12)    // 12 is actually hour 0.
                            nPMAdjust -= 12;

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
                else
                {
                    if (nHour >= 0 && nHour <= 24)
                    {
                        InternalDateTime = InternalDateTime.AddHours(nHour - InternalDateTime.Hour);
                    }

                    if (nHour > 2)
                    {
                        DTHour.Text = InternalDateTime.ToString(CrossAssemblyNotifyContainer.TwentyFourHourMode ? "HH" : "hh");
                        DTMinute.Focus();
                    }
                    else
                    {
                        DTHour.SelectionChanged -= new RoutedEventHandler(TBSelectionChanged);
                        DTHour.Select(1, 0);
                        DTHour.SelectionChanged += new RoutedEventHandler(TBSelectionChanged);
                    }
                }
            }
            catch (Exception)
            {
                FrameworkElement fw = (FrameworkElement)FocusManager.GetFocusedElement();
                if( fw == DTHour && DTHour.Text == "" )
                {
                    // fine.  the user hit backspace or something.
                    // we'll allow this error to ride.
                    return;
                }
                DTHour.Text = InternalDateTime.ToString(CrossAssemblyNotifyContainer.TwentyFourHourMode ? "HH" : "hh");
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
            DTCalendar.DisplayDate = InternalDateTime;

            DTHour.Text = InternalDateTime.ToString(CrossAssemblyNotifyContainer.TwentyFourHourMode ? "HH" : "hh");
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