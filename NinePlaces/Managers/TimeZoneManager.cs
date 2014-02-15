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
using Common.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using System.ComponentModel;
using System.Xml;
using Common;
using NinePlaces.Models;
using NinePlaces.Undo;

namespace NinePlaces.Managers
{

    public class StubDefaultTimeZone : ITimeZone
    {
        public bool Loaded
        {
            get { return false; }
        }

        public event EventHandler TimeZoneInfoUpdated;

        public double DSTOffset
        {
            get { return 0.0; }
        }

        public double Offset
        {
            get { return 0.0; }
        }

        public string TimeZoneID
        {
            get { return "America/Montreal"; }
        }

        public double OffsetAtTime(DateTime in_dtTime, bool in_bLocal = false)
        {
            return 0.0;
        }

        public DSTChangeDirection ContainsDSTChange(DateTime in_dtStartTime, DateTime in_dtEndTime, bool in_bLocal = false)
        {
            return DSTChangeDirection.DSTNone;
        }

        public bool TimeZoneEquals(ITimeZone in_Zone)
        {
            return false;
        }

    }

    public class TimeZoneManager : ITimeZoneManager
    {

        public static bool TimeZonesEqual(ITimeZone in_t1, ITimeZone in_t2)
        {
            if (in_t1 == null || in_t2 == null)
                return false;

            if (in_t1.TimeZoneID == in_t2.TimeZoneID)
                return true;

            if (!in_t1.Loaded || !in_t2.Loaded)
                return false;

            return (in_t1.DSTOffset == in_t2.DSTOffset &&
                in_t2.Offset == in_t2.Offset);
        }

        /// <summary>
        /// Notification timer.  We absorb up notifications and send them on a timer, so as not to 
        /// overwhelm everyone.
        /// </summary>
        protected DispatcherTimer m_oTimer = new DispatcherTimer();


        private List<ITimeZoneSegment> m_arAllSegments = new List<ITimeZoneSegment>();
        /// <summary>
        /// All of the registered timezone segments.
        /// Note that this will dynamically compute up a list of timezone segments
        /// whenver m_arAllSegments is cleared.
        /// </summary>
        public IList<ITimeZoneSegment> AllSegments
        {
            get
            {
                return m_arAllSegments;
            }
        }

        private ITimeZoneDriver m_tzDefaultTimeZone = null;

        /// <summary>
        /// The timezone that starts at DAteTime.MinValue.
        /// In theory, the user's 'home' timezone.
        /// </summary>
        public ITimeZoneDriver DefaultTimeZone
        {
            get
            {
                return m_tzDefaultTimeZone;
            }
            set
            {
                if (m_tzDefaultTimeZone != null)
                    UnregisterIconVM(m_tzDefaultTimeZone, false);

                m_tzDefaultTimeZone = value;
                m_tzDefaultTimeZoneSeg.TimeZoneObj = m_tzDefaultTimeZone.TimeZone;

                RegisterIconVM(m_tzDefaultTimeZone);
            }
        }

        /// <summary>
        /// The initial (DateTime.MinValue) timezone segment.
        /// This never gets removed - there is always a timezone
        /// that starts at DateTime.MinValue.
        /// </summary>
        private ITimeZoneSegment m_tzDefaultTimeZoneSeg = null;


        /// <summary>
        /// Notification to the outside world that something about the
        /// timezone list has changed - be it a new timezone, reordered,
        /// or simply start/end change.
        /// </summary>
        public event TimeZonesChangedEventHandler TimeZonesChanged;

        /// <summary>
        /// We use this to accumulate information when we notify on the
        /// timer.
        /// </summary>
        private TimeZonesChangedEventArgs m_oNotifyEventArgs = null;
        private object g_NotifyLock = new object();

        public TimeZoneManager()
        {
            // create up our default segment.
            m_tzDefaultTimeZoneSeg = new TimeZoneSegment();
            m_tzDefaultTimeZoneSeg.TimeZoneObj = new StubDefaultTimeZone();
            TimeZoneSegment.FirstSeg = m_tzDefaultTimeZoneSeg;
            m_tzDefaultTimeZoneSeg.TimeZoneSegmentChanged += new SegmentChangedEventHandler(TimeZoneSegmentChanged);

            m_arAllSegments.Add(m_tzDefaultTimeZoneSeg);

            //
            // lets set up our default timezone.
            // eventually, this should be set based on user location.
            //
            SimpleTimeZoneDriver driver = new SimpleTimeZoneDriver();
            driver.TimeZone = new StubDefaultTimeZone();
            driver.Start = DateTime.MinValue;
            DefaultTimeZone = driver;

            // and start the notification timer.
            m_oTimer.Interval = new TimeSpan(0, 0, 0, 0, 25);
            m_oTimer.Tick += new EventHandler(TimerTick);
        }

        /// <summary>
        /// Given a timezone identifier, this will return a TimeZoneObj object - 
        /// creating it, if necessary.
        /// </summary>
        /// <param name="in_strName"></param>
        /// <returns></returns>
        public static ITimeZone GetTimeZone(string in_strName)
        {
            App.AssemblyLoadManager.Requires("REST_XML_Model.xap");
            return TimeZone.GetTimeZone(in_strName);
        }

        public bool m_bNotify = true;
        public bool NotifyOnZoneChange
        {
            get
            {
                return m_bNotify;
            }
            set
            {
                if (m_bNotify != value)
                {
                    m_bNotify = value;

                    if (m_bNotify)
                    {
                        RejigSegments();
                        if (m_oNotifyEventArgs != null)
                            m_oTimer.Start();
                    }
                    else
                        m_oTimer.Stop();
                }
            }
        }

        /// <summary>
        /// Event handler for the segmentchanged notification.
        /// This will fill out the notifyeventargs object and then
        /// start the timer for notification.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void TimeZoneSegmentChanged(object sender, SegmentChangedEventArgs args)
        {
            ITimeZoneSegment s = sender as ITimeZoneSegment;

            if (args.ZoneChanged)
            {
                TriggerNotify(true, true);
            }
            else if (args.PropertiesChanged && !m_oNotifyEventArgs.CountChanged)
            {
                TriggerNotify(false, true);         
            }
        }

        public void TriggerNotify( bool in_bZonesChanged, bool in_bPropsChanged )
        {
            lock (g_NotifyLock)
            {
                if (m_oNotifyEventArgs == null)
                    m_oNotifyEventArgs = new TimeZonesChangedEventArgs();

                if (in_bZonesChanged)
                {
                    m_oNotifyEventArgs.OrderChanged = true;
                    m_oNotifyEventArgs.CountChanged = true;
                    m_oNotifyEventArgs.StartEndTimesChanged = true;
                }
                else if (in_bPropsChanged && !m_oNotifyEventArgs.CountChanged)
                {
                    m_oNotifyEventArgs.StartEndTimesChanged = true;
                }

                if( NotifyOnZoneChange )
                    m_oTimer.Start();
            }
        }

        /// <summary>
        /// Notify timer tick handler.
        /// When this ticks, we fire our notification.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void TimerTick(object sender, EventArgs e)
        {
            m_oTimer.Stop();
            lock (g_NotifyLock)
            {
                if (m_oNotifyEventArgs == null)
                    return;

                if (TimeZonesChanged != null)
                {
                    TimeZonesChanged.Invoke(this, m_oNotifyEventArgs);
                    m_oNotifyEventArgs = null;
                }
            }
        }

        void TimeZoneSlave_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!(sender is ITimeZoneSlave) || sender == m_oAdjustReentrancyPrevent)
                return;

            if (e.PropertyName == "DockTime")
            {
                ITimeZoneSlave s = sender as ITimeZoneSlave;

                // this will update the child in its segment.
                // if it no longer belongs there, the segment will know.
                if (s.CurrentTimeZoneMasterSegment != null)
                {
                    Bounds b = (s.CurrentTimeZoneMasterSegment as TimeZoneSegment).IsWithinBounds(s);
                    ITimeZoneSegment seg = s.CurrentTimeZoneMasterSegment;
                    while( b == Bounds.Before || b == Bounds.After )
                    {
                        if (b == Bounds.Before)
                            seg = seg.PrevSeg;
                        else if( b == Bounds.After )
                            seg = seg.NextSeg;

                        b = (seg as TimeZoneSegment).IsWithinBounds(s);
                    }

                    if (seg != s.CurrentTimeZoneMasterSegment)
                    {
                        s.CurrentTimeZoneMasterSegment = seg;
                    }
                }
            }
        }

        public ITimeZoneSlave m_oAdjustReentrancyPrevent = null;

        void MutableTimeZoneProperties_ExposesTimeZoneChanged(object sender, EventArgs e)
        {
            RejigSegments();
        }

        List<ITimeZoneSlave> m_arAllVMs = new List<ITimeZoneSlave>();
        public List<T> SortedVMs<T>()
        {
            return (from u in m_arAllVMs
                    where (u is T)
                    orderby u.DockTime
                    select (T)u).ToList();
        }

        public List<T> VMs<T>()
        {
            return (from u in m_arAllVMs
                    where (u is T)
                    select (T)u).ToList();
        }

        void tz_TimeZonePropertiesChanged(object sender, EventArgs e)
        {
            ITimeZoneDriver d = sender as ITimeZoneDriver;
            if (d == null || sender == m_oAdjustReentrancyPrevent )
                return;

            IMutableTimeZoneProperties dMut = sender as IMutableTimeZoneProperties;
            if (dMut != null && !dMut.ExposesTimeZone)
                return;
            RejigSegments();
        }

        private List<ITimeZone> m_arOrderedSegmentZones = null;
        public void RejigSegments()
        {
            if (!NotifyOnZoneChange)
                return;
            using (new AutoComplexUndo())
            {
                //
                // ah, an icon has changed position.
                //
                List<ITimeZoneDriver> arSortedDrivers = SortedVMs<ITimeZoneDriver>();
                List<ITimeZoneSlave> arSortedSlaves = SortedVMs<ITimeZoneSlave>();

                TimeZoneSegment tzCurSeg = m_tzDefaultTimeZoneSeg as TimeZoneSegment;
                //
                // first, figure out the contiguous segments.
                //
                ITimeZone tzPrevZone = null;
                string strPrevSeg = string.Empty;
                List<ITimeZone> arSegmentZones = new List<ITimeZone>();       // distinct segments.
                foreach (ITimeZoneDriver tz in arSortedDrivers)
                {
                    if (string.IsNullOrEmpty(tz.TimeZoneID))
                    {
                        continue;
                    }
                    else if( tzPrevZone == null || !TimeZonesEqual( tzPrevZone, tz.TimeZone ))
                    {
                        // this is a distinct, new segment.
                        strPrevSeg = tz.TimeZoneID;
                        arSegmentZones.Add(tz.TimeZone);
                    }
                    else if (TimeZonesEqual(tzPrevZone, tz.TimeZone))
                    {
                        // this is a continuation of the previous segment.
                        continue;
                    }
                }

                bool bOrderChanged = false;
                bool bCountChanged = m_arOrderedSegmentZones == null || m_arOrderedSegmentZones.Count != arSegmentZones.Count;
                //
                // do we need to continue?
                //
                if (m_arOrderedSegmentZones != null && m_arOrderedSegmentZones.Count == arSegmentZones.Count)
                {
                    // ok, same number.. same order?
                    for (int i = 0; i < m_arOrderedSegmentZones.Count; i++)
                    {
                        if (!TimeZonesEqual(m_arOrderedSegmentZones[i], arSegmentZones[i]))
                            bOrderChanged = true;
                    }

                }
                m_arOrderedSegmentZones = new List<ITimeZone>(arSegmentZones);

                //
                // ok, now we need to figure out all of the zones that we want to keep.
                //
                Dictionary<ITimeZone, List<ITimeZoneSegment>> dictSegmentsToKeep = new Dictionary<ITimeZone, List<ITimeZoneSegment>>();
                Dictionary<ITimeZone, List<ITimeZoneSegment>> dictSegmentsToRemove = new Dictionary<ITimeZone, List<ITimeZoneSegment>>();
                foreach (ITimeZoneSegment seg in AllSegments)
                {
                    if (arSegmentZones.Contains(seg.TimeZoneObj))
                    {
                        if (!dictSegmentsToKeep.ContainsKey(seg.TimeZoneObj))
                            dictSegmentsToKeep.Add(seg.TimeZoneObj, new List<ITimeZoneSegment>());

                        dictSegmentsToKeep[seg.TimeZoneObj].Add(seg);
                        arSegmentZones.Remove(seg.TimeZoneObj);
                    }
                    else
                    {
                        (seg as TimeZoneSegment).DestroySeg();
                        seg.TimeZoneSegmentChanged -= new SegmentChangedEventHandler(TimeZoneSegmentChanged);
                    }
                }

                foreach (ITimeZone oMissingZone in arSegmentZones)
                {
                    if (!dictSegmentsToKeep.ContainsKey(oMissingZone))
                        dictSegmentsToKeep.Add(oMissingZone, new List<ITimeZoneSegment>());

                    TimeZoneSegment tz = new TimeZoneSegment();
                    tz.TimeZoneSegmentChanged += new SegmentChangedEventHandler(TimeZoneSegmentChanged);
                    tz.TimeZoneID = oMissingZone.TimeZoneID;
                    dictSegmentsToKeep[oMissingZone].Add(tz);
                }

                m_arAllSegments.Clear();

                TimeZoneSegment curSeg = null;
                foreach (ITimeZoneDriver s in arSortedDrivers)
                {
                    if (curSeg == null || (!string.IsNullOrEmpty(s.TimeZoneID) && !TimeZonesEqual(s.TimeZone, curSeg.TimeZoneObj)))
                    {
                        //
                        // next seg!
                        //
                        //if (curSeg != null)
                        //    curSeg.CalcDriver();

                        ITimeZoneSegment prevseg = curSeg;
                        curSeg = dictSegmentsToKeep[s.TimeZone][0] as TimeZoneSegment;
                        curSeg.NextSeg = curSeg.PrevSeg = null;
                        curSeg.AllDrivers.Clear();
                        dictSegmentsToKeep[s.TimeZone].RemoveAt(0);
                        curSeg.SegmentDriver = s;

                        if (prevseg != null)
                        {
                            curSeg.PrevSeg = prevseg;
                            prevseg.NextSeg = curSeg;
                            s.CurrentTimeZoneMasterSegment = prevseg;
                        }
                        m_arAllSegments.Add(curSeg);
                    }

                    curSeg.AddDriver(s);
                }

                //curSeg.CalcDriver();

                //
                // Now, important: we're going to update the drivers first.
                // to be consistent, we update each DRIVER to its 'currentmastersegment'
                // and then below we update the slaves.
                //
                TimeZoneSegment.FirstSeg = curSeg = m_tzDefaultTimeZoneSeg as TimeZoneSegment;
                foreach (ITimeZoneSlave s in arSortedDrivers)
                {
                    if (s is SimpleTimeZoneDriver)
                    {
                        s.CurrentTimeZoneMasterSegment = m_tzDefaultTimeZoneSeg;
                        continue;
                    }

                    Bounds b = curSeg.IsWithinBounds(s);
                    System.Diagnostics.Debug.Assert(b != Bounds.Before);

                    while (b == Bounds.After)
                    {
                        System.Diagnostics.Debug.Assert(curSeg.NextSeg != null);
                        curSeg = curSeg.NextSeg as TimeZoneSegment;
                        b = curSeg.IsWithinBounds(s);
                    }

                    if (s.CurrentTimeZoneMasterSegment != curSeg)
                        s.CurrentTimeZoneMasterSegment = curSeg;
                }

                //
                // ok, lets go through the loop again.
                // this time, we'll update the slaves.
                //
                TimeZoneSegment.FirstSeg = curSeg = m_tzDefaultTimeZoneSeg as TimeZoneSegment;
                foreach (ITimeZoneSlave s in arSortedSlaves)
                {
                    if (s is ITimeZoneDriver)
                        continue;
                    Bounds b = curSeg.IsWithinBounds(s);
                    System.Diagnostics.Debug.Assert(b != Bounds.Before);

                    while (b == Bounds.After)
                    {
                        System.Diagnostics.Debug.Assert(curSeg.NextSeg != null);
                        curSeg = curSeg.NextSeg as TimeZoneSegment;
                        b = curSeg.IsWithinBounds(s);
                    }

                    if (s.CurrentTimeZoneMasterSegment != curSeg)
                        s.CurrentTimeZoneMasterSegment = curSeg;
                }
            
                TriggerNotify(bOrderChanged || bCountChanged, true);
            }

            (m_tzDefaultTimeZoneSeg as TimeZoneSegment).ValidateStart();
        }


        public void RegisterIconVM(ITimeZoneSlave in_iIconViewModel)
        {
            RegisterIconVM(in_iIconViewModel, true);
        }
        public void RegisterIconVM(ITimeZoneSlave in_iIconViewModel, bool in_bRejig)
        {
            m_arAllVMs.Add(in_iIconViewModel);
            INotifyPropertyChanged p = in_iIconViewModel as INotifyPropertyChanged;
            if (p != null)
                p.PropertyChanged += new PropertyChangedEventHandler(TimeZoneSlave_PropertyChanged);

            ITimeZoneDriver tz = in_iIconViewModel as ITimeZoneDriver;
            if (tz != null)
                tz.TimeZonePropertiesChanged += new EventHandler(tz_TimeZonePropertiesChanged);

            IMutableTimeZoneProperties tzMut = in_iIconViewModel as IMutableTimeZoneProperties;
            if (tzMut != null)
                tzMut.ExposesTimeZoneChanged += new EventHandler(MutableTimeZoneProperties_ExposesTimeZoneChanged);

            RejigSegments();
        }

        public void UnregisterIconVM(ITimeZoneSlave in_iIconViewModel)
        {
            UnregisterIconVM(in_iIconViewModel, true);
        }

        public void UnregisterIconVM(ITimeZoneSlave in_iIconViewModel, bool in_bRejig)
        {
            m_arAllVMs.Remove(in_iIconViewModel);
            INotifyPropertyChanged p = in_iIconViewModel as INotifyPropertyChanged;
            if( p != null )
                p.PropertyChanged -= new PropertyChangedEventHandler(TimeZoneSlave_PropertyChanged);

            ITimeZoneDriver tz = in_iIconViewModel as ITimeZoneDriver;
            if (tz != null)
                tz.TimeZonePropertiesChanged -= new EventHandler(tz_TimeZonePropertiesChanged);

            IMutableTimeZoneProperties tzMut = in_iIconViewModel as IMutableTimeZoneProperties;
            if (tzMut != null)
                tzMut.ExposesTimeZoneChanged -= new EventHandler(MutableTimeZoneProperties_ExposesTimeZoneChanged);

            if( in_bRejig )
                RejigSegments();
        }

        /// <summary>
        /// Given a time, returns the OFFSET at that time.
        /// 
        /// Note that this is much slower than calling directly on the 
        /// timezone object itself.  If you have access to the timezone 
        /// object, call that.
        /// </summary>
        /// <param name="in_dtTime"></param>
        /// <param name="in_bLocal"></param>
        /// <returns></returns>
        public double OffsetAtTime(DateTime in_dtTime, bool in_bLocal = false)
        {
            
            ITimeZoneSegment i = m_tzDefaultTimeZoneSeg;
            do
            {
                if (i.TransitionStart < in_dtTime && i.End >= in_dtTime)
                    return i.TimeZoneObj.OffsetAtTime(in_dtTime, in_bLocal);

                i = i.NextSeg;
            } while (i != null);

            throw new Exception("OffsetAtTime should ALWAYS get a hit");
        }

        /// <summary>
        /// Given a range, returns the direction of a DST change.
        /// 
        /// Note that this is much slower than calling directly on the
        /// timezone object itself.  If you have access to the timezone,
        /// call that.
        /// </summary>
        /// <param name="in_dtTime"></param>
        /// <param name="in_dtEndTime"></param>
        /// <param name="in_bLocal"></param>
        /// <returns></returns>
        public DSTChangeDirection ContainsDSTChange(DateTime in_dtTime, DateTime in_dtEndTime, bool in_bLocal = false)
        {
            ITimeZoneSegment i = m_tzDefaultTimeZoneSeg;
            do
            {
                if (i.TransitionStart <= in_dtTime && i.End >= in_dtTime)
                    return i.TimeZoneObj.ContainsDSTChange(in_dtTime, in_dtEndTime, in_bLocal);

                i = i.NextSeg;
            } while (i != null);

            throw new Exception("ContainsDSTChange should ALWAYS get a hit");
        }
    }
    
}
