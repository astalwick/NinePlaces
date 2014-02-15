using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Common.Interfaces;
using System.Collections.Generic;

namespace NinePlaces
{
	public partial class WhereAmI : UserControl
	{
		public WhereAmI()
		{
			// Required to initialize variables
			InitializeComponent();
		}

        private IVacationViewModel m_oVacation = null;
        public IVacationViewModel Vacation
        {
            get
            {
                return m_oVacation;
            }
            set
            {
                if( m_oVacation != null )   // unsubscribe!
                    m_oVacation.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(m_oVacation_PropertyChanged);

                m_oVacation = value;

                if( value != null )
                    m_oVacation.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(m_oVacation_PropertyChanged);
            }

        }

        public ITimeline ParentTimeline
        {
            set
            {
                value.StartTimeChanged += new EventHandler(value_StartTimeChanged);
                value.DurationChanged += new EventHandler(value_DurationChanged);
                value.TimelineSizeChanged += new SizeChangedEventHandler(value_TimelineSizeChanged);
                value.ActiveVacationChanged += new EventHandler(value_ActiveVacationChanged);
            }
        }

        void value_ActiveVacationChanged(object sender, EventArgs e)
        {
            Vacation = (sender as ITimeline).ActiveVacation;
        }

        void value_TimelineSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // the timeline size has changed - that means our 'PixelsPerHour' count 
            // is off.  recalc.
            m_dPixelsPerHour = ((double)LayoutRoot.ActualWidth) / Duration.TotalHours;
            PositionElements();
        }

        void value_DurationChanged(object sender, EventArgs e)
        {
            // the duration of the timeline has changed.  
            // update both the duration and starttime.
            Duration = (sender as ITimeline).Duration;
            StartTime = (sender as ITimeline).StartTime;
        }

        void value_StartTimeChanged(object sender, EventArgs e)
        {
            // the starttime has changed.
            StartTime = (sender as ITimeline).StartTime;
        }

        private double m_dPixelsPerHour = 0.0;
        void TimelineSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // the timeline size has changed - that means our 'PixelsPerHour' count 
            // is off.  recalc.
            m_dPixelsPerHour = ((double)LayoutRoot.ActualWidth) / Duration.TotalHours;
            PositionElements();
        }

        private static SolidColorBrush FontColor = new SolidColorBrush(Color.FromArgb(255, 33, 159, 204));
        private Queue<TextBlock> m_arUnusedElements = new Queue<TextBlock>();
        private Dictionary<int, TextBlock> m_arTextBlockElements = new Dictionary<int,TextBlock>();
        private void PositionElements()
        {
            if (Vacation == null)
            {
                foreach (TextBlock tb in m_arTextBlockElements.Values)
                {
                    tb.Visibility = Visibility.Collapsed;
                    m_arUnusedElements.Enqueue(tb);
                }
                m_arTextBlockElements.Clear();
                return;
            }


            List<int> arKeys = new List<int>(m_arTextBlockElements.Keys);
            List<IArrivalLocationProperties> arLocations = new List<IArrivalLocationProperties>(Vacation.SortedChildren<IArrivalLocationProperties>());

            for (int i = arLocations.Count-1; i >= 0; i--)
            {
                if (arLocations[i] is IMutableIconProperties && !((arLocations[i] as IMutableIconProperties).CurrentClass == IconClass.Transportation))
                    arLocations.RemoveAt(i);
            }

            for (int i = 0; i < arLocations.Count; i++ )
            {
                IArrivalLocationProperties loc = arLocations[i];

                if( loc is IDurationIconViewModel )
                {
                    IDurationIconViewModel vm = loc as IDurationIconViewModel;
                    if (vm.EndTime < StartTime)
                    {
                        continue;
                    }
                    if (vm.DockTime > StartTime + Duration)
                        continue;

                    // ok, we're in the view.
                    TextBlock tb = null;
                    if( !m_arTextBlockElements.TryGetValue( vm.UniqueID, out tb ))
                    {
                        // nope.
                        if( m_arUnusedElements.Count > 0 )
                        {
                            tb = m_arUnusedElements.Dequeue();
                            tb.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            tb = new TextBlock();
                            tb.FontSize = 16;
                            tb.Foreground = FontColor;
                            tb.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                            ElementCanvas.Children.Add(tb);
                        }

                        m_arTextBlockElements.Add(vm.UniqueID, tb);
                    }
                    else
                    {
                        arKeys.Remove(vm.UniqueID);
                    }

                    tb.Text = loc.ArrivalCity;
                    double dActualWidth = tb.ActualWidth;

                    // ok, we've got tb.
                    DateTime dtStart = vm.DockTime > StartTime ? vm.DockTime : StartTime;

                    DateTime dtNextStart = i+1 == arLocations.Count ? DateTime.MaxValue : ( arLocations[i+1] as IDockableViewModel).DockTime ;

                    DateTime dtEnd = dtNextStart < StartTime + Duration ? dtNextStart : StartTime + Duration;

                    double dWidth = (dtEnd - dtStart).TotalHours * m_dPixelsPerHour;
                    if (dWidth < dActualWidth)
                        tb.Text = string.Empty;
                    else
                    {
                        double dX = (dtStart - StartTime).TotalHours * m_dPixelsPerHour;

                        Canvas.SetLeft(tb, dX + (dWidth / 2.0 - dActualWidth / 2.0));
                    }
                    //tb.Width = dWidth;
                }
            }

            foreach (int n in arKeys)
            {
                TextBlock tb = m_arTextBlockElements[n];

                tb.Visibility = Visibility.Collapsed;
                m_arTextBlockElements.Remove(n);
                m_arUnusedElements.Enqueue(tb);
            }
        }

        private DateTime m_dtStartTime = DateTime.MinValue;
        /// <summary>
        /// StartTime of the timeline.
        /// </summary>
        public DateTime StartTime
        {
            get
            {
                return m_dtStartTime;
            }
            set
            {
                if (m_dtStartTime != value)
                {
                    m_dtStartTime = value;

                    // if start tiem has changed, we need to update the piselsperday and
                    // reposition the icons on the dockline to reflect the change.
                    m_dPixelsPerHour = ((double)LayoutRoot.ActualWidth) / Duration.TotalHours;
                    PositionElements();
                }
            }
        }


        private TimeSpan m_tsDuration = TimeSpan.MinValue;
        /// <summary>
        /// Duration of the timeline.
        /// </summary>
        public TimeSpan Duration
        {
            get
            {
                return m_tsDuration;
            }
            set
            {
                if (m_tsDuration != value)
                {
                    m_tsDuration = value;
                }
            }
        }

        void TimelineDurationChanged(object sender, EventArgs e)
        {
            // the duration of the timeline has changed.  
            // update both the duration and starttime.
            Duration = (sender as ITimeline).Duration;
            StartTime = (sender as ITimeline).StartTime;
        }

        void TimelineStartTimeChanged(object sender, EventArgs e)
        {
            // the starttime has changed.
            StartTime = (sender as ITimeline).StartTime;
        }

        void m_oVacation_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SortedLocations")
            {
                // ah, gotta update.
                PositionElements();
            }
        }
	}
}