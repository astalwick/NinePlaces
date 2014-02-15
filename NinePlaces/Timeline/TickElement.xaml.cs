using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Common.Interfaces;
using System.Windows.Data;
using System.Globalization;
using NinePlaces.Helpers;
namespace NinePlaces
{
    /// <summary>
    /// Possible durations available to tick elements.
    /// </summary>
    public enum TickElementDuration
    {
        ArbitraryTimeSpan = 0,
        Month = 1,
        Day = 2,
        Hour = 3,
        Unknown = 4
    }

    public interface ITickElement : IInterfaceElement
    {
        ITimeZoneSegment RepresentedSegment { get; }

        TickElementDuration TickDuration { get; }
        DateTime TickStartTime { get; set; }            // UTC!
        DateTime TickEndTime { get; set; }              // UTC!

        DateTime OffsetStart { get; }            // to be used internall only.  
#warning put these into an IPrivateTickElement interface
        DateTime OffsetEnd { get; }              
        event TickElementClickedEventHandler Clicked;

        ITickElement ParentTick { get; set; }
        void DoUpdate();

        void Reconnect(ITickRibbon in_oParent);
        void Disconnect();
    }

    public partial class TickElement : UserControl, ITickElement, ITimeZoneTickElement
    {
        // Interface elements
        #warning these should be template-ized.
        private static SolidColorBrush g_oDefaultWeekDayBrush = new SolidColorBrush(Color.FromArgb(0, 101, 101, 101));
        private static SolidColorBrush g_oDefaultWeekendBrush = new SolidColorBrush(Color.FromArgb(20, 101, 101, 101));
        private static SolidColorBrush g_oHoverBrush = new SolidColorBrush(Color.FromArgb(30, 101, 101, 101));

        protected ITickRibbon m_oParentRibbon = null;     // this is a reference to our OWNER ribbon.
        protected Dictionary<int, ITickElement> m_dictChildren = new Dictionary<int, ITickElement>();         // a dictionary of a child tickelements (from 1 to 31 or 1 to 12, depending on duration).
        protected TextBlock m_tbHolidayTextBlock = null;
        protected TickElementDuration m_td = TickElementDuration.Unknown;     // the duration of this tickelement.

        public event TickElementClickedEventHandler Clicked;
        protected double dOffset = 0.0;

        /// <summary>
        /// The duration level of this tick element.
        /// </summary>
        public virtual TickElementDuration TickDuration 
        {
            get
            {
                return m_td;
            }
            private set 
            {
                if (m_td != value)
                    m_td = value;
            }
        }

        public virtual ITimeZoneSegment RepresentedSegment
        {
            get { return ParentTick.RepresentedSegment; }
            set { }
        }

        private DateTime m_dtLastUpdateStartTime = DateTime.Now;
        private DateTime m_dtLastUpdateEndTime = DateTime.Now;
        private TickVisibilityDetailLevels m_visLastUpdateDetailLevel = TickVisibilityDetailLevels.Years;
        private int m_nLastUpdateSubticks = -1;
        private int m_nLastUpdateStartSubtick = -1;
        private int m_nLastUpdateEndSubtick = -1;

        private DSTChangeDirection m_eDSTChangeDirection = DSTChangeDirection.DSTNone;
        Queue<ITickElement> m_arPool = new Queue<ITickElement>();

        public static int g_nUpdates = 0;
        public void DoUpdate()
       { 

            //
            // here's the idea.  a single ticksegment of arbitrary length is the root of the hierarchy of tickelements.
            // the ticksegment is responsible for OFFSETTING its children.  the children tickelements KNOW NOTHING of
            // local time or utc time.  they are told their time by their parent (the ticksegment).  SO: for
            // the ticksegment, TickStartTime will be UTC.  OffsetStart will be Local.  for all children, the two 
            // will be the same.
            //

            DateTime dtOwnerStart = ParentTick != null ? ParentTick.OffsetStart : m_oParentRibbon.StartTime;
            DateTime dtOwnerEnd = ParentTick != null ? ParentTick.OffsetEnd : m_oParentRibbon.EndTime;

            if (TickEndTime /*OffsetEnd*/ < dtOwnerStart ||         // JUST CHANGED THIS
                TickStartTime /*OffsetStart*/ > dtOwnerEnd )          // IF IT CAUSES WEIRDNESS CHANGE OFFSETSTART AND OFFSET END TO TICKSTART AND TICKEND
            {
                m_nLastUpdateEndSubtick = -1;
                m_nLastUpdateStartSubtick = -1;
                m_nLastUpdateSubticks = -1;
                m_dtLastUpdateStartTime = DateTime.Now;
                m_dtLastUpdateEndTime = DateTime.Now;
                m_visLastUpdateDetailLevel = TickVisibilityDetailLevels.Years;

                Visibility = Visibility.Collapsed;
                RemoveAllChildren();
                return;
            }

            
            if (OffsetStart == m_dtLastUpdateStartTime &&
                OffsetEnd == m_dtLastUpdateEndTime &&
                m_visLastUpdateDetailLevel == m_oParentRibbon.DetailLevel )
                return;     // nothing has changed.
            
            if (m_visLastUpdateDetailLevel != m_oParentRibbon.DetailLevel)
            {
                
                if (!CheckTickVisibility(TickDuration))
                {
                    // we're invisible
                    Visibility = Visibility.Collapsed;

                    m_nLastUpdateEndSubtick = -1;
                    m_nLastUpdateStartSubtick = -1;
                    m_nLastUpdateSubticks = -1;

                    // and our children are toast.
                    RemoveAllChildren();

                    m_dtLastUpdateStartTime = OffsetStart;
                    m_dtLastUpdateEndTime = OffsetEnd;
                    m_visLastUpdateDetailLevel = m_oParentRibbon.DetailLevel;
                    return;
                }
            }

            Visibility = Visibility.Visible;

            bool bDSTHandled = true;
            if (TickDuration == TickElementDuration.Day || TickDuration == TickElementDuration.Month)
            {
                m_eDSTChangeDirection = RepresentedSegment.TimeZoneObj.ContainsDSTChange(OffsetStart, OffsetEnd, true);
                bDSTHandled = false;
            }

            if (TickDuration != TickElementDuration.Hour && 
                CheckTickVisibility(TickDuration + 1) && 
                (OffsetStart != m_dtLastUpdateStartTime ||
                OffsetEnd != m_dtLastUpdateEndTime || 
                m_visLastUpdateDetailLevel != m_oParentRibbon.DetailLevel))
            {
                g_nUpdates++;
                int nStartTick, nEndTick;
                int nSubTicks = SubTicks(out nStartTick, out nEndTick);
                if (nSubTicks != m_nLastUpdateSubticks)
                {
                    // we need to add or remove a column.
                    UpdateColumns(nSubTicks);
                }

                //
                // ok, update the columnwidths.
                //
                if (m_nLastUpdateSubticks == -1 || nStartTick < m_nLastUpdateStartSubtick || nEndTick > m_nLastUpdateEndSubtick )
                {
                    for (int i = nStartTick + 1; i < nEndTick - 1; i++)
                    {
                        if (TickDuration == TickElementDuration.Month && m_eDSTChangeDirection != DSTChangeDirection.DSTNone && m_oParentRibbon.DetailLevel > TickVisibilityDetailLevels.MediumDayNames)
                        {  
                            // this is a DST Change month. 
                            // need to check if this date contains the dst change.  if so, then it will have an odd 
                            // number of hours.

                            // note that we DO NOT CARE to be this precise when we're zoomed way out.  that is why we're checking the detail level.
                            LayoutRoot.ColumnDefinitions[i - nStartTick].Width = new GridLength(WidthFromSubTick(i - nStartTick, nSubTicks), GridUnitType.Star);
                        }
                        else
                        {
                            LayoutRoot.ColumnDefinitions[i - nStartTick].Width = new GridLength(1.0, GridUnitType.Star);
                        }
                    }
                }

                // always update the first and last column.
                LayoutRoot.ColumnDefinitions[0].Width = new GridLength(WidthFromSubTick(0, nSubTicks), GridUnitType.Star);
                LayoutRoot.ColumnDefinitions[nSubTicks - 1].Width = new GridLength(WidthFromSubTick(nSubTicks - 1, nSubTicks), GridUnitType.Star);

                //
                // NOW: our actual subticks.
                // do we need to REMOVE or ADD any subticks?
                    
                // first, lets pull everything OUT that we can.
                if (m_nLastUpdateStartSubtick != -1 && 
                    ( m_nLastUpdateEndSubtick != nEndTick || m_nLastUpdateStartSubtick != nStartTick))
                {
                    ITickElement t = null;
                    for (int i = nEndTick; i < m_nLastUpdateEndSubtick; i++)
                    {
                        if (m_dictChildren.TryGetValue(i, out t))
                        {
                            m_arPool.Enqueue(t);
                            m_dictChildren.Remove(i);
                        }
                    }

                    for (int i = m_nLastUpdateStartSubtick; i < nStartTick && i < m_nLastUpdateEndSubtick; i++)
                    {
                        if (m_dictChildren.TryGetValue(i, out t))
                        {
                            m_arPool.Enqueue(t);
                            m_dictChildren.Remove(i);
                        }
                    }
                }

                // great, we've got a pool to draw from in CREATING any new ticks, if necessary.

                DateTime dtCur = OffsetStart;
                DateTime dtNext = AddDuration(dtCur, TickDuration + 1);
                for (int i = nStartTick; i < nEndTick; i++)
                {
                    ITickElement tChild = null;
                    m_dictChildren.TryGetValue(i, out tChild);



                    if (tChild != null)
                    {
                        // we've already got a child in this cell.  nothign to do except
                        // continue to the next cell.
                        if (dtNext > OffsetEnd)
                            tChild.TickEndTime = OffsetEnd;
                        else
                            tChild.TickEndTime = dtNext;
                        tChild.TickStartTime = dtCur;
                        Grid.SetColumn(tChild.Control, i - nStartTick);
                    }
                    else
                    {
                        if (m_arPool.Count > 0)
                        {
                            tChild = m_arPool.Dequeue();
                        }
                        else
                        {
                            // ok great, we have a cell that is empty and needs a new tickelement.
                            // lets ask the tickbag for one.
                            tChild = m_oParentRibbon.TickBag.GetTick(TickDuration + 1);
                            tChild.ParentTick = this;
                            tChild.Clicked += new TickElementClickedEventHandler(ElementClicked);
                            // set visibility right away (bugs if you don't!)
                            tChild.Control.Visibility = Visibility.Visible;
                            // update the start time (do this after the tick has been made visible!)
                            LayoutRoot.Children.Add(tChild.Control);
                        }

                        if (dtNext > TickEndTime)
                            tChild.TickEndTime = OffsetEnd;
                        else
                            tChild.TickEndTime = dtNext;
                        tChild.TickStartTime = dtCur;

                        System.Diagnostics.Debug.Assert(dtCur > DateTime.MinValue);

                        // add the tick to our lists!
                        m_dictChildren.Add(i, tChild);
                        Grid.SetColumn(tChild.Control, i - nStartTick);
                    }

                    if (TickDuration == TickElementDuration.Day && !bDSTHandled && m_eDSTChangeDirection != DSTChangeDirection.DSTNone)
                    {
                        DSTChangeDirection dChange = RepresentedSegment.TimeZoneObj.ContainsDSTChange(dtCur, dtNext, true);
                        if (dChange != DSTChangeDirection.DSTNone)
                        {
                            if (dChange == DSTChangeDirection.DSTJumpBackward)
                            {
                                // jumpbackward means we REPEAT dtCur.
                                bDSTHandled = true;
                                continue;
                            }
                            else
                            {
                                bDSTHandled = true;
                                dtCur = dtNext;
                                dtNext = AddDuration(dtCur, TickDuration + 1);
                                // jumpforward means we skip an hour!
                            }
                        }
                    }

                    // finally - continue to the next tick!
                    dtCur = dtNext;
                    dtNext = AddDuration(dtCur, TickDuration + 1);
                }

                ITickElement tToRemove = null;
                while (m_arPool.Count > 0)
                {
                    tToRemove = m_arPool.Dequeue();
                    LayoutRoot.Children.Remove(tToRemove.Control);
                    tToRemove.Clicked -= new TickElementClickedEventHandler(ElementClicked);
                    m_oParentRibbon.TickBag.GiveTick(tToRemove);
                }

                m_nLastUpdateEndSubtick = nEndTick;
                m_nLastUpdateStartSubtick = nStartTick;
                m_nLastUpdateSubticks = nSubTicks;
            }

            if (m_visLastUpdateDetailLevel != m_oParentRibbon.DetailLevel || OffsetStart != m_dtLastUpdateStartTime)
                UpdateText();

            if (m_visLastUpdateDetailLevel != m_oParentRibbon.DetailLevel)
                UpdateTextPosition();

            
            //UpdateColor();
            //UpdateHolidayState();

            foreach (int nKey in m_dictChildren.Keys)
            {
                m_dictChildren[nKey].DoUpdate();
            }

            m_dtLastUpdateStartTime = OffsetStart;
            m_dtLastUpdateEndTime = OffsetEnd;
            m_visLastUpdateDetailLevel = m_oParentRibbon.DetailLevel;
        }

        protected DateTime m_dtStart = DateTime.MinValue;         // the starttime of the tickelement
        public virtual DateTime TickStartTime
        {
            get
            {
                return m_dtStart;
            }
            set
            {
                if (m_dtStart != value)
                {
                    m_dtStart = value;
                    UpdateTickHeight();
                }
            }
        }

        protected DateTime m_dtEnd = DateTime.MaxValue;         // the starttime of the tickelement
        public virtual DateTime TickEndTime
        {
            get
            {
                return m_dtEnd;
            }
            set
            {
                if (m_dtEnd != value)
                {
                    m_dtEnd = value;
                }
            }
        }

        public virtual DateTime OffsetStart
        {
            get
            {
                if (TickDuration != TickElementDuration.ArbitraryTimeSpan)
                    return TickStartTime;
                else
                    return TickStartTime.AddHours(RepresentedSegment.TimeZoneObj.OffsetAtTime(TickStartTime));
            }
        }

        public virtual DateTime OffsetEnd
        {
            get
            {
                if (TickDuration != TickElementDuration.ArbitraryTimeSpan)
                    return TickEndTime;
                else
                    return TickEndTime.AddHours(RepresentedSegment.TimeZoneObj.OffsetAtTime(TickEndTime));
            }
        }

        protected ITickElement m_oParentTick = null;
        public ITickElement ParentTick { get; set; }

        public TickElement(ITickRibbon in_oParent, TickElementDuration in_td)
        {
            InitializeComponent();

            // we need a parent!
            System.Diagnostics.Debug.Assert(in_oParent != null);
            m_oParentRibbon = in_oParent;

            // set the tick duration - once set, we cannot set again.
            TickDuration = in_td;

            Loaded += new RoutedEventHandler(TickElement_Loaded);
        }

        protected void TickElement_Loaded(object sender, RoutedEventArgs e)
        {
            // first of all, prepare all of the text elements in this tick elelemtn.
            InitializeText();
            if (!CheckTickVisibility(TickDuration))
            {
                Visibility = Visibility.Collapsed;
                return;
            }

            // we need to be notified when the language changes.
            Common.CrossAssemblyNotifyContainer.LocalizationChange += new EventHandler(CrossAssemblyNotifyContainer_LocalizationChange);
            Common.CrossAssemblyNotifyContainer.HourFormatChange += new Common.HourFormatEventHandler(CrossAssemblyNotifyContainer_HourFormatChange);

            if (TickDuration < TickElementDuration.Hour)
                TickCanvas.MouseLeftButtonUp += new MouseButtonEventHandler(TickElement_MouseLeftButtonUp);

            TickCanvas.MouseLeftButtonDown += new MouseButtonEventHandler(TickCanvas_MouseLeftButtonDown);
            // update the datetime text.
            UpdateText();

            UpdateHitTestState();

            // update our tick height.
            UpdateTickHeight();
        }

        void CrossAssemblyNotifyContainer_HourFormatChange(object sender, Common.HourFormatEventArgs args)
        {
            UpdateText();
        }

        /// <summary>
        /// Called to prepare the tick element text.  Different tick element durations
        /// have different ways of displaying text.  This initializes the fonts and sizes for
        /// each duration level.
        /// </summary>
        protected void InitializeText()
        {
            DateText.Visibility = Visibility.Collapsed;
            DateText.FontFamily = new FontFamily("Portable User Interface");
            switch (TickDuration)
            {
                case TickElementDuration.Month:
                    DateText.Foreground = new SolidColorBrush(Color.FromArgb(255, 101, 101, 101));
                    DateText.FontSize = 10;
                    break;
                case TickElementDuration.Day:
                    DateText.Foreground = new SolidColorBrush(Color.FromArgb(255, 101, 101, 101));
                    DateText.FontSize = 10;
                    break;
                case TickElementDuration.Hour:
                    DateText.Foreground = new SolidColorBrush(Color.FromArgb(255, 101, 101, 101));
                    DateText.FontSize = 8;
                    break;
            }
        }

        protected void TickElement_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!App.Dragging && IsHittable(TickDuration))
            {
                if (Clicked != null)
                {
                    Clicked(this, new TickElementClickedEventArgs(TickStartTime, TickEndTime - TickStartTime));
                }
                e.Handled = true;
                
            }
            e.Handled = false;
        }

        protected bool m_bHovered = false;
        protected void TickElement_MouseLeave(object sender, MouseEventArgs e)
        {
            m_bHovered = false;
            UpdateColor();
        }

        protected void TickElement_MouseEnter(object sender, MouseEventArgs e)
        {
            m_bHovered = true;
            UpdateColor();
        }

        protected void TickCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = false;
        }

        protected void TickVisibilityChanged(object sender, TickVisibilityChangedEventArgs args)
        {
            DoUpdate();
        }

        protected void CrossAssemblyNotifyContainer_LocalizationChange(object sender, EventArgs e)
        {
            UpdateText();
        }

        protected bool m_bRegistered = false;
        /// <summary>
        /// Something has changed - we need to decide whether or not we're hittable,
        /// and if we are, register for the correct events.
        /// </summary>
        protected void UpdateHitTestState()
        {
            if (TickDuration >= TickElementDuration.Hour || m_oParentRibbon == null )
            {
                IsHitTestVisible = false;
                return;
            }

            if (!m_bRegistered && IsHittable(TickDuration))
            {
                m_bRegistered = true;
                TickCanvas.MouseLeave += new MouseEventHandler(TickElement_MouseLeave);
                TickCanvas.MouseEnter += new MouseEventHandler(TickElement_MouseEnter);
            }
            else if (m_bRegistered && !IsHittable(TickDuration))
            {
                m_bRegistered = false;
                TickCanvas.MouseLeave -= new MouseEventHandler(TickElement_MouseLeave);
                TickCanvas.MouseEnter -= new MouseEventHandler(TickElement_MouseEnter);
            }

            if (!IsHittable(TickDuration) && m_bHovered)
            {
                m_bHovered = false;
                TickCanvas.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            }

            if (TickDuration != TickElementDuration.Day ||
                m_oParentRibbon.DetailLevel >= TickVisibilityDetailLevels.DayNumbers && TickDuration == TickElementDuration.Day)
                IsHitTestVisible = true;
            else
                IsHitTestVisible = false;
        }

        /// <summary>
        /// Updates the color state of this tick element.
        /// </summary>
        protected void UpdateColor()
        {
            if (m_bHovered)
                TickCanvas.Background = g_oHoverBrush;
            else
                TickCanvas.Background = g_oDefaultWeekDayBrush;
        }


        /// <summary>
        /// If this is a holidate, weekend, or any other special day,
        /// thie function will update the tick state to reflect that.
        /// </summary>
        protected void UpdateHolidayState()
        {
            if (TickDuration == TickElementDuration.Day && OffsetStart.Date == DateTime.Now.Date)
            {
                // today!
                m_tbHolidayTextBlock = new TextBlock();
                m_tbHolidayTextBlock.SetBinding(TextBlock.TextProperty, new Binding("StringLibrary.Today") { Source = App.LocalizedStrings, Mode = BindingMode.OneWay });

                m_tbHolidayTextBlock.FontFamily = new FontFamily("Portable User Interface");
                m_tbHolidayTextBlock.Foreground = new SolidColorBrush(Color.FromArgb(255, 70, 70, 70));
                m_tbHolidayTextBlock.FontSize = 12;
                m_tbHolidayTextBlock.FontWeight = FontWeights.ExtraBold;

                Canvas.SetTop(m_tbHolidayTextBlock, 25);

                TickCanvas.Children.Add(m_tbHolidayTextBlock);
            }
        }

        /// <summary>
        /// Adds/removes the grid columns in which child tick elements will
        /// be put.
        /// </summary>
        protected void UpdateColumns(int in_nExpectedColumns)
        {
            int nCur = LayoutRoot.ColumnDefinitions.Count;
            if (nCur > in_nExpectedColumns)
            {
                for (int i = 0; i < nCur - in_nExpectedColumns; i++)
                {
                    LayoutRoot.ColumnDefinitions.RemoveAt(nCur - (i + 1));
                }
            }
            else if (nCur < in_nExpectedColumns)
            {
                for (int i = 0; i < in_nExpectedColumns - nCur; i++)
                {
                    ColumnDefinition cdToAdd = new ColumnDefinition();
                    LayoutRoot.ColumnDefinitions.Add(cdToAdd);
                }
            }

            if( in_nExpectedColumns > 0 )
                Grid.SetColumnSpan(TickCanvas, in_nExpectedColumns);
        }

        /// <summary>
        /// Tick indicators change size slightly as you zoom in and out. 
        /// This updates the size.
        /// </summary>
        protected void UpdateTickHeight()
        {
            if (!IsStartOfDuration())
            {
                Tick.Visibility = Visibility.Collapsed;
                return;
            }

            Tick.Visibility = Visibility.Visible;

            double dHeight = 0.0;
            switch (TickDuration)
            {
                case TickElementDuration.ArbitraryTimeSpan:
                case TickElementDuration.Unknown:
                case TickElementDuration.Month:
                    dHeight = 12.0;
                    break;
                case TickElementDuration.Day:
                    dHeight = 6.0;
                    break;
                case TickElementDuration.Hour:
                    dHeight = 4.0;
                    break;
            }
            Tick.Height = dHeight;
            /*
            double dHeight = 0.0;
            if (TickDuration == TickElementDuration.ArbitraryTimeSpan)
            {
                dHeight = ActualHeight;
            }
            if (TickDuration == TickElementDuration.Month && OffsetStart.Month == 1 && OffsetStart.Day == 1)
            {
                // the first month tick is ALWAYS 20 pixels high.
                dHeight = 20.0;
            }
            else
            {
                //
                // this is a bit of a dodgy algorithm to scale the height up and down.
                //
                double dTotalDays = 31.0;
                switch (TickDuration)
                {
                    case TickElementDuration.ArbitraryTimeSpan:
                    case TickElementDuration.Month:
                    case TickElementDuration.Unknown:
                        dTotalDays = 31.0;
                        break;
                    case TickElementDuration.Day:
                        dTotalDays = 1.0;
                        break;
                    case TickElementDuration.Hour:
                        dTotalDays = 1.0 / 24.0;
                        break;
                }

                dHeight = m_oParentRibbon.PixelsPerHour * dTotalDays / 4.0;
                if (TickDuration == TickElementDuration.Day && OffsetStart.DayOfWeek == DayOfWeek.Sunday)
                    dHeight *= 1.5;

                if (dHeight > 15.0)
                    dHeight = 15.0;
                else if (dHeight < 4.0)
                    dHeight = 4.0;

            }
            Tick.Height = dHeight;*/
        }

        /// <summary>
        /// Text is displayed at a different location depending on detail level.
        /// Update the text position.
        /// </summary>
        protected void UpdateTextPosition()
        {
            //
            // this is a simple function to determine which line we should draw our
            // date text one. 
            //
            switch (TickDuration)
            {
                //case TickElementDuration.Year:
                //    break;      // nothing to do here - we always display '2009'
                case TickElementDuration.Month:
                    if (m_oParentRibbon.DetailLevel < TickVisibilityDetailLevels.DayNumbers)
                        Canvas.SetTop(DateText, 0);
                    else if (m_oParentRibbon.DetailLevel < TickVisibilityDetailLevels.LongDayNames)
                        Canvas.SetTop(DateText, 10);
                    break;
                case TickElementDuration.Day:
                    if (m_oParentRibbon.DetailLevel < TickVisibilityDetailLevels.HourNumbers)
                        Canvas.SetTop(DateText, 0);
                    else
                        Canvas.SetTop(DateText, 10);
                    break;
                case TickElementDuration.Hour:
                    Canvas.SetTop(DateText, 0);
                    break;
            }
        }


        protected bool IsStartOfDuration()
        {
            // returns true only when the offset start
            // is equal to the expected start of the given tickduration.
            // 
            // example: if tickduration is MONTH, then the DayOfMonth should be 1 and the time should be 12:00am.

            switch (TickDuration)
            {
                case TickElementDuration.ArbitraryTimeSpan:
                case TickElementDuration.Unknown:
                    return false;
                case TickElementDuration.Month:
                    return OffsetStart.Day == 1 && OffsetStart.Hour == 0 && OffsetStart.Minute == 0;
                case TickElementDuration.Day:
                    return OffsetStart.Hour == 0 && OffsetStart.Minute == 0;
                case TickElementDuration.Hour:
                    return OffsetStart.Minute == 0;
            }
            return false;
        }

        /// <summary>
        /// Text is displayed in a different format depending on detail leevel.
        /// </summary>
        protected void UpdateText()
        {
            if (!IsStartOfDuration())
            {
                DateText.Visibility = Visibility.Collapsed;
                return;
            }

            switch (TickDuration)
            {
                //case TickElementDuration.Year:
                //    DateText.Visibility = Visibility.Visible;
                //    break;      // nothing to do here - we always display '2009'
                case TickElementDuration.Month:
                    if (m_oParentRibbon.DetailLevel >= TickVisibilityDetailLevels.LongDayNames ||
                         (m_oParentRibbon.DetailLevel == TickVisibilityDetailLevels.Years && OffsetStart.Month != 1) ||
                         Visibility == Visibility.Collapsed)
                    {
                        DateText.Visibility = Visibility.Collapsed;
                        return;
                    }
                    else
                        DateText.Visibility = Visibility.Visible;

                    if (m_oParentRibbon.DetailLevel == TickVisibilityDetailLevels.Years)
                        DateText.Text = OffsetStart.ToString("yyyy");
                    else if (m_oParentRibbon.DetailLevel <= TickVisibilityDetailLevels.MonthsOnly)
                        DateText.Text = OffsetStart.ToCultureAwareString("MMM").Substring(0, 1);
                    else if (m_oParentRibbon.DetailLevel <= TickVisibilityDetailLevels.ShortMonthNames)
                        DateText.Text = OffsetStart.ToCultureAwareString("MMM");
                    else
                        DateText.Text = OffsetStart.ToCultureAwareString("MMMM");

                    if (OffsetStart.Month == 1 && m_oParentRibbon.DetailLevel > TickVisibilityDetailLevels.Years)
                        DateText.Text += "\r\n" + OffsetStart.ToCultureAwareString("yyyy");

                    break;
                case TickElementDuration.Day:
                    if (m_oParentRibbon.DetailLevel < TickVisibilityDetailLevels.DayNumbers)
                    {
                        DateText.Visibility = Visibility.Collapsed;
                        return;
                    }
                    else
                        DateText.Visibility = Visibility.Visible;

                    if (m_oParentRibbon.DetailLevel <= TickVisibilityDetailLevels.DayNumbers)
                        DateText.Text = OffsetStart.ToCultureAwareString("%d");
                    else if (m_oParentRibbon.DetailLevel <= TickVisibilityDetailLevels.ShortDayNames)
                        DateText.Text = OffsetStart.ToCultureAwareString("d ddd");
                    else if (m_oParentRibbon.DetailLevel <= TickVisibilityDetailLevels.MediumDayNames)
                        DateText.Text = OffsetStart.ToCultureAwareString("d dddd");
                    else 
                        DateText.Text = OffsetStart.ToCultureAwareString("dddd, MMMM d");
                    break;
                case TickElementDuration.Hour:
                    if (m_oParentRibbon.DetailLevel < TickVisibilityDetailLevels.HourNumbers)
                    {
                        DateText.Visibility = Visibility.Collapsed;
                        return;
                    }
                    else
                        DateText.Visibility = Visibility.Visible;

                    if (m_oParentRibbon.DetailLevel <= TickVisibilityDetailLevels.HourNumbers)
                        DateText.Text = OffsetStart.ToCultureAwareString( App.TwentyFourHourMode ? "%H" :"%h");
                    else if (m_oParentRibbon.DetailLevel <= TickVisibilityDetailLevels.ShortHourNames)
                        DateText.Text = OffsetStart.ToCultureAwareString(App.TwentyFourHourMode ? "%H" : "h tt");
                    else if (m_oParentRibbon.DetailLevel <= TickVisibilityDetailLevels.LongHourNames)
                        DateText.Text = OffsetStart.ToCultureAwareString(App.TwentyFourHourMode ? "H:mm" : "h:mm tt");
                    break;
            }
        }

        /// <summary>
        /// Reconnects a tick element to its parent tick ribbon.
        /// Called when a previously constructed tick element is brought
        /// back from the tick bag.
        /// </summary>
        /// <param name="in_oParent"></param>
        public void Reconnect(ITickRibbon in_oParent)
        {
            //
            // ok, we're a tick that existed once, and was 
            // sent back to the tickbag for recycling.  well,
            // now we're being pulled out and need to be reconnected.
            //

            // set the parent.
            m_oParentRibbon = in_oParent;

            // wire up the events.
            //m_oParentRibbon.TickVisibilityChanged += new TickVisibilityChangedEventHandler(TickVisibilityChanged);
            if (!CheckTickVisibility(TickDuration))
                return; 

            Common.CrossAssemblyNotifyContainer.LocalizationChange += new EventHandler(CrossAssemblyNotifyContainer_LocalizationChange);
            Common.CrossAssemblyNotifyContainer.HourFormatChange += new Common.HourFormatEventHandler(CrossAssemblyNotifyContainer_HourFormatChange);
            
            // update the tick height.
            UpdateTickHeight();

            // Update the text, so that the font height/text is correct.
            InitializeText();
            UpdateText();
            UpdateTextPosition();

            // datetime text should take care of itself when our caller
            // sets the starttime of this tickelement.
            UpdateHitTestState();
        }
        
        /// <summary>
        /// Called when a tick is no longer necessary (no longer being used)
        /// by its parent tickribbon.
        /// </summary>
        public virtual void Disconnect()
        {
            //
            // we're no longer needed, and we're now in the tickbag. 
            // that means that we should no longer pay attention to messages.
            // in fact, we should disconnect entirely from external soruces.
            //
            Loaded -= new RoutedEventHandler(TickElement_Loaded);
            if (m_tbHolidayTextBlock != null)
            {
                TickCanvas.Children.Remove(m_tbHolidayTextBlock);
                m_tbHolidayTextBlock = null;
            }

            ParentTick = null;
            // no, we're not visible anymore!
            Visibility = Visibility.Collapsed;
            DateText.Visibility = Visibility.Collapsed;

            if (m_dictChildren.Count != 0)
            {
                // remove children.
                foreach (ITickElement iChild in m_dictChildren.Values)
                {
                    //
                    // give it in to the tickbag.
                    //
                    LayoutRoot.Children.Remove(iChild.Control);
                    m_oParentRibbon.TickBag.GiveTick(iChild);
                    iChild.Clicked -= new TickElementClickedEventHandler(ElementClicked);
                    iChild.Disconnect();
                }
            }
            m_dictChildren.Clear();

            
            // reset our state variables.
            m_nLastUpdateEndSubtick = -1;
            m_nLastUpdateStartSubtick = -1;
            m_nLastUpdateSubticks = -1;
            m_dtLastUpdateStartTime = DateTime.Now;
            m_dtLastUpdateEndTime = DateTime.Now;
            m_visLastUpdateDetailLevel = TickVisibilityDetailLevels.Years;
            TickStartTime = DateTime.MinValue;
            TickEndTime = DateTime.MinValue;

            Common.CrossAssemblyNotifyContainer.LocalizationChange -= new EventHandler(CrossAssemblyNotifyContainer_LocalizationChange);
            Common.CrossAssemblyNotifyContainer.HourFormatChange -= new Common.HourFormatEventHandler(CrossAssemblyNotifyContainer_HourFormatChange);

            // disconnect from our (former) parent.
            m_oParentRibbon = null;

        }

        /// <summary>
        /// Returns true if the tick element is visible at the current ribbon detaillevel.
        /// </summary>
        /// <param name="in_tdToCheck"></param>
        /// <returns></returns>
        public bool CheckTickVisibility(TickElementDuration in_tdToCheck)
        {
            if (m_oParentRibbon == null)
                return false;

            TickVisibilityDetailLevels tDetail = m_oParentRibbon.DetailLevel;
            switch (in_tdToCheck)
            {
                case TickElementDuration.ArbitraryTimeSpan:
                    return true;
                case TickElementDuration.Hour:
                    return tDetail >= TickVisibilityDetailLevels.LongDayNames;
                case TickElementDuration.Day:
                    return tDetail >= TickVisibilityDetailLevels.LongMonthNames;
                case TickElementDuration.Month:
                    return true;
            }

            return false;
        }

        protected void ElementClicked(object sender, TickElementClickedEventArgs args)
        {
            if (Clicked != null)
                Clicked(sender, args);
        }

        /// <summary>
        /// Destroys/removes all child ticks.
        /// </summary>
        public void RemoveAllChildren()
        {
            if (TickDuration == TickElementDuration.Hour || TickDuration == TickElementDuration.Unknown)
                return;     // no grid to destroy

            foreach (ITickElement i in m_dictChildren.Values)
            {
                // all we need to do is remove our children.  they'll clean up 
                // themselves when the disconnect.
                LayoutRoot.Children.Remove(i.Control);
                // recycle or child off into the tickbag.
                m_oParentRibbon.TickBag.GiveTick(i);
                i.Clicked -= new TickElementClickedEventHandler(ElementClicked);
                i.Disconnect();
            }
            m_dictChildren.Clear();
        }

        /// <summary>
        /// Returns true if the current tick is 'hittable' (clickable)
        /// at the current detaillevel.
        /// </summary>
        /// <param name="in_td"></param>
        /// <returns></returns>
        protected bool IsHittable(TickElementDuration in_td)
        {
            if (m_oParentRibbon == null)
                return false;

            //if (in_td == TickElementDuration.Year ||
            //     m_oParentRibbon.DetailLevel >= TickVisibilityDetailLevels.DayNumbers && in_td == TickElementDuration.Day ||
            //     m_oParentRibbon.DetailLevel < TickVisibilityDetailLevels.DayNumbers && in_td == TickElementDuration.Month)
            //    return true;
            return false;
        }

        /// <summary>
        /// Returns the number of CHILD TICKS this element contains.
        /// </summary>
        /// <returns></returns>
        /// 
        protected int SubTicks()
        {
            int n, n1;
            return SubTicks(out n, out n1);
        }
        protected int SubTicks(out int out_nStartTick, out int out_nEndTick)
        {
            out_nStartTick = 0;
            out_nEndTick = 0;

            switch (TickDuration)
            {
                case TickElementDuration.ArbitraryTimeSpan:
                    {
                        int nMonths = 0;
                        DateTime dtStart = new DateTime(OffsetStart.Year, OffsetStart.Month, 1, 0, 0, 0);
                        DateTime dtEnd = dtStart;
                        do
                        {
                            nMonths++;
                            dtEnd = dtEnd.AddMonths(1);
                        } while (dtEnd < OffsetEnd);

                        out_nStartTick = dtStart.Year * 12 + dtStart.Month;
                        out_nEndTick = dtEnd.Year * 12 + dtEnd.Month;
                        return nMonths;
                    }
                case TickElementDuration.Month:
                    {
                        int nDays = 0;
                        DateTime dtStart = new DateTime(OffsetStart.Year, OffsetStart.Month, OffsetStart.Day, 0, 0, 0);
                        DateTime dtEnd = dtStart;
                        do
                        {
                            nDays++;
                            dtEnd = dtEnd.AddDays(1);
                        } while (dtEnd < OffsetEnd);
                        out_nStartTick = OffsetStart.Day;
                        out_nEndTick = OffsetStart.Day + nDays;
                        return nDays;
                    }
                case TickElementDuration.Day:
                    {
                        //
                        // THIS needs to detect whether or not the day is a DAYLIGHT SAVINGS TIME CHANGE DAY.
                        // If so, that could result in FEWER or MORE hours per day.
                        //
                        int nHours = 0;
                        DateTime dtStart = new DateTime(OffsetStart.Year, OffsetStart.Month, OffsetStart.Day, OffsetStart.Hour, 0, 0);

                        int nDSTAdjust = 0;
                        if( m_eDSTChangeDirection != DSTChangeDirection.DSTNone )
                        {
                            if (m_eDSTChangeDirection == DSTChangeDirection.DSTJumpBackward)
                                nDSTAdjust++;
                            else
                                nDSTAdjust--;
                        }

                        DateTime dtEnd = dtStart;
                        do
                        {
                            nHours++;
                            dtEnd = dtEnd.AddHours(1);
                        } while (dtEnd < OffsetEnd);
                        out_nStartTick = OffsetStart.Hour;
                        out_nEndTick = OffsetStart.Hour + nHours + nDSTAdjust;
                        return nHours + nDSTAdjust;
                    }
                case TickElementDuration.Hour:
                    return 0;
            }
            return 0;
        }

        protected double WidthFromSubTick(int in_nTick, int in_nExpectedColumns)
        {
            if (in_nExpectedColumns < 0)
                throw new Exception();      // avoid warnig CA2233

            //if (in_nTick == 0 || in_nTick == in_nExpectedColumns - 1)
            {
                DateTime dtSubTickStart;
                DateTime dtSubTickEnd;
                if (in_nTick == 0)
                {
                    dtSubTickStart = OffsetStart;
                    dtSubTickEnd = AddDuration(dtSubTickStart, TickDuration + 1);
                    if (dtSubTickEnd > OffsetEnd)
                        dtSubTickEnd = OffsetEnd;
                }
                else if (in_nTick == in_nExpectedColumns - 1)
                {
                    dtSubTickEnd = OffsetEnd;
                    switch (TickDuration)
                    {
                        case TickElementDuration.ArbitraryTimeSpan:
                            dtSubTickStart = new DateTime(OffsetEnd.Year, OffsetEnd.Month, 1);
                            break;
                        case TickElementDuration.Month:
                            dtSubTickStart = OffsetEnd.Date;
                            break;
                        case TickElementDuration.Day:
                            dtSubTickStart = new DateTime(OffsetEnd.Year, OffsetEnd.Month, OffsetEnd.Day, OffsetEnd.Hour, 0, 0);
                            break;
                        case TickElementDuration.Hour:
                            dtSubTickStart = new DateTime(OffsetEnd.Year, OffsetEnd.Month, OffsetEnd.Day, OffsetEnd.Hour, OffsetEnd.Minute, 0);
                            break;
                        default:
                            throw new Exception();
                    }

                    if (dtSubTickStart == OffsetEnd)
                        return 1.0;
                }
                else
                {
                    dtSubTickStart = AddDuration(OffsetStart, TickDuration + 1 );
                    switch (TickDuration)
                    {
                        case TickElementDuration.ArbitraryTimeSpan:
                            throw new Exception();
                        case TickElementDuration.Month:
                            dtSubTickStart = dtSubTickStart.AddDays(in_nTick - 1);
                            break;
                        case TickElementDuration.Day:
                            dtSubTickStart = dtSubTickStart.AddHours(in_nTick - 1);
                            break;
                        case TickElementDuration.Hour:
                            dtSubTickStart = dtSubTickStart.AddMinutes(in_nTick - 1);
                            break;
                        default:
                            throw new Exception();
                    }

                    dtSubTickEnd = AddDuration(dtSubTickStart, TickDuration + 1);
                }



                TimeSpan ts = dtSubTickEnd - dtSubTickStart;
                
                switch (TickDuration)
                {
                    case TickElementDuration.ArbitraryTimeSpan:
                        {
                            // how long is this month supposed to be?
                            DateTime dtTest = new DateTime(OffsetStart.Year, OffsetStart.Month, 1);
                            double dTotalDays = (dtTest.AddMonths(1) - dtTest).TotalDays;
                            return ts.TotalDays / dTotalDays;
                        }
                    case TickElementDuration.Month:
                        {
                            double dTotalHours = ts.TotalHours;
                            if (m_eDSTChangeDirection != DSTChangeDirection.DSTNone)
                            {
                                // ok, this MONTH contains a DST change.
                                // does the subtick represented by dtSubTickEnd/dtSubTickStart contain 
                                // the dst change?  if so, the timespan must be adjusted.
                                DSTChangeDirection dir = RepresentedSegment.TimeZoneObj.ContainsDSTChange(dtSubTickStart, dtSubTickEnd, true);
                                if (dir != DSTChangeDirection.DSTNone)
                                {
                                    dTotalHours += dir == DSTChangeDirection.DSTJumpBackward ? 1.0 : -1.0;
                                }
                            }

                            // a normal day is 24 hours.
                            // note that if it's a DST day, it could be 23 hours or 25 hours.
                            return dTotalHours / 24.0;
                        }
                    case TickElementDuration.Day:
                        {
                            // an hour should be 60 minutes.
                            return ts.TotalMinutes / 60.0;
                        }
                    case TickElementDuration.Hour:
                        {
                            return 0;
                        }
                    default:
                        return 0;
                }
            }

            return 1.0;
        }

        /// <summary>
        /// Helper function to to quickly add a certain amount of time to the tick, 
        /// and return the 
        /// </summary>
        /// <param name="in_dtStart"></param>
        /// <param name="in_tdToAdd"></param>
        /// <returns></returns>
        public static DateTime AddDuration(DateTime in_dtStart, TickElementDuration in_tdToAdd)
        {
            if (in_dtStart == DateTime.MaxValue || in_dtStart == DateTime.MinValue)
                return in_dtStart;
            
            switch (in_tdToAdd)
            {
                case TickElementDuration.ArbitraryTimeSpan:
                    {
                        return new DateTime(in_dtStart.Year, in_dtStart.Month, 1, 0, 0, 0).AddMonths(1);
                    }
                case TickElementDuration.Month:
                    {
                        return new DateTime(in_dtStart.Year, in_dtStart.Month, 1, 0, 0, 0).AddMonths(1);
                    }
                case TickElementDuration.Day:
                    {
                        return new DateTime(in_dtStart.Year, in_dtStart.Month, in_dtStart.Day, 0, 0, 0).AddDays(1);
                    }
                case TickElementDuration.Hour:
                    {
                        return new DateTime(in_dtStart.Year, in_dtStart.Month, in_dtStart.Day, in_dtStart.Hour, 0, 0).AddHours(1);
                    }
            }

            throw new Exception();
        }

        #region IInterfaceElement Members

        public Control Control
        {
            get { return this as Control; }
        }

        #endregion

    }
}
