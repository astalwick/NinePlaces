using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using NinePlaces.Helpers;
using Common.Interfaces;
using System.Linq;

namespace NinePlaces
{
    public enum TickVisibilityDetailLevels
    {
        Years = 1,
        MonthsOnly,
        ShortMonthNames,
        LongMonthNames,
        DayNumbers,
        ShortDayNames,
        MediumDayNames,
        LongDayNames,
        HourNumbers,
        ShortHourNames,
        LongHourNames
    }

    public interface IDurationBasedElement
    {
        DateTime StartTime { get; set; }
        DateTime EndTime { get; }
        TimeSpan Duration { get; set; }

        event EventHandler DurationChanged;
        event EventHandler StartTimeChanged;
    }

    public interface ITickRibbon : IDurationBasedElement
    {
        double PixelsPerHour { get; }
        TickBag TickBag { get; }
        TickVisibilityDetailLevels DetailLevel { get; }
        ITimeline ParentTimeline { set; }

        event TickVisibilityChangedEventHandler TickVisibilityChanged;
        event TickElementClickedEventHandler Clicked;
    }

    public partial class TickRibbon : UserControl, ITickRibbon
    {
        #region IDurationBasedElement Members
        public event TickVisibilityChangedEventHandler TickVisibilityChanged;
        public event TickElementClickedEventHandler Clicked;
        public event EventHandler DurationChanged;
        public event EventHandler StartTimeChanged;

        private double m_dPixelsPerHour = 0.0;
        /// <summary>
        /// Depending on the viewport timespan, days can have a different
        /// 'width' in pixels.
        /// </summary>
        public double PixelsPerHour
        {
            get
            {
                return m_dPixelsPerHour;
            }
            set
            {
                m_dPixelsPerHour = value;

                //
                // thresholds for visibilities:
                //
                NotifyVisibility();
            }
        }

        private TickBag m_oTickBag;
        /// <summary>
        /// The TickBag constructs and recycles TickElements.
        /// </summary>
        public TickBag TickBag
        {
            get
            {
                return m_oTickBag;
            }
        }

        private double m_dTickWidthMinimum = 3.0 / 24.0;

        private DateTime m_dtStartTime = DateTime.MinValue;
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
                    StartDate.Text = StartTime.ToCultureAwareString("MMMM yyyy");
                    EndDate.Text = EndTime.ToCultureAwareString("MMMM yyyy");
                    UpdateSegments();
                    UpdateTicks();
                    if (StartTimeChanged != null)
                        StartTimeChanged.Invoke(this, new EventArgs());
                }
            }
        }

        public DateTime EndTime
        {
            get
            {
                return StartTime + Duration;
            }
        }

        private TimeSpan m_tsDuration = TimeSpan.MinValue;
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
                    UpdateSegments();
                    UpdateTicks();
                    if (DurationChanged != null)
                        DurationChanged.Invoke(this, new EventArgs());
                }
            }
        }
 
        private TickVisibilityDetailLevels m_tDetailLevel = TickVisibilityDetailLevels.Years;
        public TickVisibilityDetailLevels DetailLevel
        {
            get
            {
                return m_tDetailLevel;
            }
            private set
            {
                m_tDetailLevel = value;
            }
        }

        public ITimeline ParentTimeline
        {
            set
            {
                value.StartTimeChanged += new EventHandler(Timeline_StartTimeChanged);
                value.DurationChanged += new EventHandler(Timeline_DurationChanged);
            }
        }
        
        public TickRibbon()
        {
            InitializeComponent();
            m_oTickBag = new TickBag(this);
            Loaded += new RoutedEventHandler(TickRibbon_Loaded);
            App.TimeZones.TimeZonesChanged += new TimeZonesChangedEventHandler(TimeZones_TimeZonesChanged);
            Common.CrossAssemblyNotifyContainer.LocalizationChange += new EventHandler(CrossAssemblyNotifyContainer_LocalizationChange);
        }

        void TimeZones_TimeZonesChanged(object sender, TimeZonesChangedEventArgs args)
        {
            UpdateSegments();

            if (args.StartEndTimesChanged)
                UpdateTicks();
        }

        private Dictionary<ITimeZoneSegment, ITimeZoneTickElement> m_dictSegToSegment = new Dictionary<ITimeZoneSegment, ITimeZoneTickElement>();

        private bool IsInBounds(ITimeZoneSegment in_oToCheck)
        {
            return ((in_oToCheck.Start < EndTime && in_oToCheck.Start >= StartTime) ||
                        (in_oToCheck.End > StartTime && in_oToCheck.End <= EndTime) ||
                        (in_oToCheck.Start < StartTime && in_oToCheck.End > EndTime));
        }

        private void UpdateSegments()
        {
            IEnumerable<ITimeZoneSegment> arAllTZChanges = App.TimeZones.AllSegments;
            List<ITimeZoneSegment> arToRemove = new List<ITimeZoneSegment>();
            foreach (ITimeZoneSegment iSeg in m_dictSegToSegment.Keys)
            {
                if (!arAllTZChanges.Contains(iSeg))
                {
                    arToRemove.Add(iSeg);
                }
            }

            foreach (ITimeZoneSegment iSegToRemove in arToRemove)
            {
                if (m_arChildSegments.Contains(m_dictSegToSegment[iSegToRemove]))
                {
                    TicksContainer.Children.Remove(m_dictSegToSegment[iSegToRemove].Control);
                    m_arChildSegments.Remove(m_dictSegToSegment[iSegToRemove]);
                }

                m_dictSegToSegment[iSegToRemove].Disconnect();
                m_dictSegToSegment.Remove(iSegToRemove);
            }

            foreach (ITimeZoneSegment i in arAllTZChanges)
            {
                ITimeZoneTickElement tzSeg = null;
                if (!m_dictSegToSegment.TryGetValue(i, out tzSeg))
                {
                    // need to construct a new segment and add it to the dictionary.
                    tzSeg = new TimeZoneTickElement(this, TickElementDuration.ArbitraryTimeSpan, i);

                    if (tzSeg.RepresentedSegment.End > EndTime)
                        tzSeg.TickEndTime = EndTime;
                    else
                        tzSeg.TickEndTime = tzSeg.RepresentedSegment.End;

                    if (tzSeg.RepresentedSegment.Start < StartTime)
                        tzSeg.TickStartTime = StartTime;
                    else
                        tzSeg.TickStartTime = tzSeg.RepresentedSegment.Start;

                    tzSeg.Reconnect(this);
                    m_dictSegToSegment.Add(i, tzSeg);

                    if( IsInBounds( tzSeg.RepresentedSegment ) )
                    {
                        m_arChildSegments.Add(tzSeg);
                        TicksContainer.Children.Add(tzSeg.Control);
                    }
                }
                else
                {
                    bool bIsInBounds = IsInBounds( i );
                    if (!bIsInBounds && m_arChildSegments.Contains(tzSeg))
                    {
                        m_arChildSegments.Remove(tzSeg);
                        TicksContainer.Children.Remove(tzSeg.Control);
                    }
                    else if( bIsInBounds && !m_arChildSegments.Contains(tzSeg) )
                    {
                        m_arChildSegments.Add(tzSeg);
                        TicksContainer.Children.Add(tzSeg.Control);
                    }
                }
            }
        }

        void TickRibbon_Loaded(object sender, RoutedEventArgs e)
        {
            SizeChanged += new SizeChangedEventHandler(TickRibbon_SizeChanged);
        }

        void CrossAssemblyNotifyContainer_LocalizationChange(object sender, EventArgs e)
        {
            StartDate.Text = StartTime.ToCultureAwareString("MMMM yyyy");
            EndDate.Text = EndTime.ToCultureAwareString("MMMM yyyy");
        }

        void TickRibbon_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (m_tsDuration != TimeSpan.MinValue)
                UpdateTicks();
        }

        void Timeline_DurationChanged(object sender, EventArgs e)
        {
            Duration = (sender as ITimeline).Duration;
            StartTime = (sender as ITimeline).StartTime; 
        }

        void Timeline_StartTimeChanged(object sender, EventArgs e)
        {
            StartTime = (sender as ITimeline).StartTime;
        }
        
        /// <summary>
        /// Determines the 'detail level' at a given timespan duration,
        /// and notifies out to the tickelements if the detail level has 
        /// changed.
        /// </summary>
        private void NotifyVisibility()
        {
            TickVisibilityDetailLevels tDetail = TickVisibilityDetailLevels.Years;

            if (PixelsPerHour * 4.0 > m_dTickWidthMinimum)
                tDetail++;   //MonthsOnly

            if (PixelsPerHour * 2.5 > m_dTickWidthMinimum)
                tDetail++;   //ShortMonthNames

            if (PixelsPerHour * 1.25 > m_dTickWidthMinimum)
                tDetail++;  //LongMonthNames  [January]

            if (PixelsPerHour / 7.0 > m_dTickWidthMinimum)
                tDetail++; //DayNumbers  [1]

            if (PixelsPerHour / 18.0 > m_dTickWidthMinimum)
                tDetail++; // ShortDayNames  [1 (Sat)

            if (PixelsPerHour / 25.0 > m_dTickWidthMinimum)
                tDetail++; // MediumDayNames  [1 (Saturday)]

            if (PixelsPerHour / 60.0 > m_dTickWidthMinimum)
                tDetail++; // LongDayNames [Saturday, January 10]

            if (PixelsPerHour / 150.0 > m_dTickWidthMinimum)
                tDetail++; // HourNumbers  [10am]

            if (PixelsPerHour / 400.0 > m_dTickWidthMinimum)
                tDetail++; // ShortHourNames  [10:00 AM]

            if (PixelsPerHour / 700.0 > m_dTickWidthMinimum)
                tDetail++; // ShortHourNames  [10:00 AM]

            if (tDetail != DetailLevel )
            {
                DetailLevel = tDetail;
            }
        }
        #endregion

        List< ITimeZoneTickElement > m_arChildSegments = new List< ITimeZoneTickElement >();
        private void UpdateTicks()
        {
            if (LayoutRoot.ActualWidth == 0)
                return;

            DateTime dtFirst = DateTime.MaxValue;
            ITimeZoneTickElement iToSizeSeg = null;
            foreach( ITimeZoneTickElement iFind in m_arChildSegments )
            {
                if (iFind.RepresentedSegment.Start < dtFirst)
                {
                    iToSizeSeg = iFind;
                    dtFirst = iToSizeSeg.RepresentedSegment.Start;
                }
            }

            // ok, now we need to loop through all of our displayed segments,
            // and add them as columns.  HOWEVER - we must remember the GAP
            // between an element's TransitionStart and its Start tiem.  
            // that must be represented as a timezone transition.
            int nColumn = 0;
            DateTime dtColumnStart = StartTime;
            do
            {
                if (TicksContainer.ColumnDefinitions.Count == nColumn)
                {
                    ColumnDefinition cdToAdd = new ColumnDefinition();
                    TicksContainer.ColumnDefinitions.Add(cdToAdd);
                }

                if (iToSizeSeg.TickStartTime > dtColumnStart)
                {
                    // MIND THE GAP!
                    DateTime dtEnd = iToSizeSeg.TickStartTime;

                    if (dtEnd > EndTime)
                        dtEnd = EndTime;

                    TicksContainer.ColumnDefinitions[nColumn].Width = new GridLength((dtEnd - dtColumnStart).TotalDays, GridUnitType.Star);
                    dtColumnStart = iToSizeSeg.TickStartTime;
                }
                else
                {
                    TicksContainer.ColumnDefinitions[nColumn].Width = new GridLength((iToSizeSeg.TickEndTime - iToSizeSeg.TickStartTime).TotalDays, GridUnitType.Star);
                    Grid.SetColumn(iToSizeSeg.Control, nColumn);

                    dtColumnStart = iToSizeSeg.TickEndTime;
                    ITimeZoneSegment iNextSeg = iToSizeSeg.RepresentedSegment.NextSeg;
                    if( iNextSeg != null )
                        iToSizeSeg = m_dictSegToSegment[iNextSeg];
                }

                nColumn++;
            } while (dtColumnStart < EndTime);

            // we're done allocating columns.  do we have too many?  
            // if so, lets get rid of the remainder.
            if (TicksContainer.ColumnDefinitions.Count > nColumn)
            {
                for (int i = nColumn; i < TicksContainer.ColumnDefinitions.Count; )
                {
                    TicksContainer.ColumnDefinitions.RemoveAt(i);
                }
            }

            PixelsPerHour = ((double)LayoutRoot.ActualWidth) / Duration.TotalHours;
            
            // now, set left.
            Canvas.SetLeft(TicksContainer,0);
            TicksContainer.Height = TicksCanvas.ActualHeight;
            TicksContainer.Width = (EndTime - StartTime).TotalHours * PixelsPerHour;

            // tell our children to update.
            foreach (ITimeZoneTickElement iSegment in m_arChildSegments)
            {
                iSegment.DoUpdate();
            }
        }
        
        void ElementClicked(object sender, TickElementClickedEventArgs args)
        {
            if (Clicked != null)
                Clicked(sender, args);
        }
    }

    public delegate void TickElementClickedEventHandler(object sender, TickElementClickedEventArgs args );
    public class TickElementClickedEventArgs
    {
        public TickElementClickedEventArgs(DateTime in_dtStart, TimeSpan in_tdDuration)
        {
            StartTime = in_dtStart;
            Duration = in_tdDuration;
        }

        public DateTime StartTime { get; private set; }
        public TimeSpan Duration { get; private set; }
    }

    public delegate void TickVisibilityChangedEventHandler(object sender, TickVisibilityChangedEventArgs args);
    public class TickVisibilityChangedEventArgs
    {
        /// <summary>
        /// Contstructor with bits
        /// </summary>
        /// <param name="horizontalChange">Horizontal change</param>
        /// <param name="verticalChange">Vertical change</param>
        /// <param name="mouseEventArgs">The mouse event args</param>
        public TickVisibilityChangedEventArgs(TickVisibilityDetailLevels in_detail)
        {
            Detail = in_detail;
        }

        public TickVisibilityDetailLevels Detail { get; set; }
    }
}
