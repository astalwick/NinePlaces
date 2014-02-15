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

namespace NinePlaces.Managers
{
    public enum Bounds
    {
        Within,
        Before,
        After
    }

    public class TimeZoneSegment : ITimeZoneSegment
    {
        static DispatcherTimer g_tValidateTimer = new DispatcherTimer();
        static TimeZoneSegment()
        {
            g_tValidateTimer.Interval = new TimeSpan(0, 0, 1);
            g_tValidateTimer.Tick += new EventHandler(g_tValidateTimer_Tick);
        }

        static void g_tValidateTimer_Tick(object sender, EventArgs e)
        {
            if (App.Dragging)
                return;
            TimeZoneSegment.ValidateZones(TimeZoneSegment.FirstSeg);
            g_tValidateTimer.Stop();
        }

        public double TimeZoneOffset { get { return TimeZoneObj.Offset; } }
        public double TimeZoneDSTOffset { get { return TimeZoneObj.DSTOffset; } }
        public string TimeZoneID
        {
            get
            {
                return TimeZoneObj == null ? string.Empty : TimeZoneObj.TimeZoneID;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    TimeZoneObj = null;
                }
                else
                {
                    TimeZoneObj = TimeZoneManager.GetTimeZone(value);
                }
            }
        }

        private void InvokeSegmentChanged(bool in_bZones, bool in_bProperties)
        {
            if( TimeZoneSegmentChanged != null )
                TimeZoneSegmentChanged.Invoke( this, new SegmentChangedEventArgs( in_bZones, in_bProperties ) );
        }

        void TimeZone_TimeZoneInfoUpdated(object sender, EventArgs e)
        {
            InvokeSegmentChanged(true, true);
        }

        private ITimeZone m_tzZone = null;
        public ITimeZone TimeZoneObj 
        { 
            get
            {
                return m_tzZone;
            }
            set
            {
                m_tzZone = value;
                if (m_tzZone != null)
                {
                    if (!TimeZoneObj.Loaded)
                    {
                        TimeZoneObj.TimeZoneInfoUpdated += new EventHandler(TimeZone_TimeZoneInfoUpdated);
                    }
                    else
                    {
                        //InvokeSegmentChanged(true, true);
                    }
                }
            }
        }


        public bool Destroyed { get; set; }
        /// <summary>
        /// SegmentDriver is the first icon in the whole segment.
        /// It is the one that is current in charge of the segment's start
        /// and transitionstart times.
        /// </summary>
        /// 
        public ITimeZoneDriver SegmentDriver { get; set; }
      
        public ITimeZoneSegment NextSeg { get; set; }
        public ITimeZoneSegment PrevSeg { get; set; }
        
        public static ITimeZoneSegment FirstSeg = null;

        public DateTime Start
        {
            get
            {
                if (End < SegmentDriver.Start)
                    return End;
                return SegmentDriver.Start;
            }
        }

        public DateTime TransitionStart 
        { 
            get
            {
                return SegmentDriver.TransitionStart;
            }
        }
        public DateTime End
        {
            get
            {
                if (NextSeg == null)
                    return DateTime.MaxValue;
                return NextSeg.TransitionStart;
            }
        }


        public List<ITimeZoneDriver> AllDrivers { get; set; }

        public event SegmentChangedEventHandler TimeZoneSegmentChanged;

        public TimeZoneSegment()
        {
            AllDrivers = new List<ITimeZoneDriver>();
            TimeZoneID = string.Empty;
            //Start = DateTime.MaxValue;
            //TransitionStart = DateTime.MaxValue;
            Destroyed = false;
        }

        public Bounds IsWithinBounds(ITimeZoneDriver in_tz)
        {
            if (!Destroyed && AllDrivers.Count == 0)
                return Bounds.Within;       // we have no bounds, but we can make it up.

            if (in_tz.TransitionStart < TransitionStart)
                return Bounds.Before;
            else if (in_tz.TransitionStart > End)
                return Bounds.After;

            return Bounds.Within;
        }

        public Bounds IsWithinBounds(ITimeZoneSlave in_tz)
        {
            if (!Destroyed && AllDrivers.Count == 0)
                return Bounds.Within;       // we have no bounds, but we can make it up.

            if (in_tz.DockTime <= TransitionStart)
                return Bounds.Before;
            else if (in_tz.DockTime > End)
                return Bounds.After;
            return Bounds.Within;
        }

        static void ValidateZones(ITimeZoneSegment in_eStartAt)
        {
            ITimeZoneSegment cur = in_eStartAt;
            ITimeZoneSegment last = null;
            while (cur != null) 
            {
                System.Diagnostics.Debug.Assert(cur.PrevSeg == null ? true : cur.PrevSeg.TransitionStart <= cur.TransitionStart);
                ValidateContents(cur);
                last = cur;
                cur = cur.PrevSeg;
            }

            System.Diagnostics.Debug.Assert(last.SegmentDriver is SimpleTimeZoneDriver);     // just ot verify that we make it back to the beginning.
            System.Diagnostics.Debug.Assert(last.TransitionStart == DateTime.MinValue);     // just ot verify that we make it back to the beginning.

            cur = in_eStartAt;
            while (cur != null)
            {
                System.Diagnostics.Debug.Assert(cur.NextSeg == null ? true : cur.NextSeg.TransitionStart >= cur.TransitionStart);
                ValidateContents(cur);
                last = cur;
                cur = cur.NextSeg;
            }

            System.Diagnostics.Debug.Assert(last.End == DateTime.MaxValue);
        }

        static void ValidateContents(ITimeZoneSegment in_eToValidate)
        {
            if (App.Dragging)
                return;

            // we must have a segment driver.
            System.Diagnostics.Debug.Assert(in_eToValidate.SegmentDriver != null );
            // the segment driver MUST have a timezone specified.
            System.Diagnostics.Debug.Assert(in_eToValidate.SegmentDriver.TimeZone != null);
            // we must not be empty of drivers!  we must have at least one driver in the segment.
            System.Diagnostics.Debug.Assert(in_eToValidate.AllDrivers.Count > 0);

            // if we have a nextseg, it must start AFTER this seg!
            System.Diagnostics.Debug.Assert(in_eToValidate.NextSeg == null ? true : in_eToValidate.TransitionStart <= in_eToValidate.NextSeg.TransitionStart);
            // if we have a prevseg, it must begin before this seg.
            System.Diagnostics.Debug.Assert(in_eToValidate.PrevSeg == null ? true : in_eToValidate.TransitionStart >= in_eToValidate.PrevSeg.TransitionStart);

            // if we have a prevseg, its nextseg must be this.
            System.Diagnostics.Debug.Assert(in_eToValidate.PrevSeg == null ? true : in_eToValidate.PrevSeg.NextSeg == in_eToValidate);

            // if we have a nextseg, its prevseg must be this.
            System.Diagnostics.Debug.Assert(in_eToValidate.NextSeg == null ? true : in_eToValidate.NextSeg.PrevSeg == in_eToValidate);

            DateTime dtStart = in_eToValidate.SegmentDriver.TransitionStart;
            DateTime dtEnd = (in_eToValidate as TimeZoneSegment).End;
            foreach (ITimeZoneDriver d in in_eToValidate.AllDrivers)
            {
                // either the driver's timezone is unspecified, or it is EQUAL to this segment's timezone.
                System.Diagnostics.Debug.Assert(d.TimeZone == null || d.TimeZone is StubDefaultTimeZone ||  in_eToValidate.TimeZoneObj.TimeZoneEquals( d.TimeZone ) );
                // the driver must be within the bounds of this segment.
                System.Diagnostics.Debug.Assert(d.TransitionStart >= dtStart && d.TransitionStart <= dtEnd);
                
                // three possibilities: 
                // 1) we're validating the firstseg, in which case the master segment is FirstSeg.
                // 2) we're not the segment driver, in which case, the master segment must be this segment.
                // 3) we ARE the segment driver, in which case, the master segment must be the previous segment
                System.Diagnostics.Debug.Assert((in_eToValidate == FirstSeg && in_eToValidate.SegmentDriver == d && d.CurrentTimeZoneMasterSegment == FirstSeg) || 
                                                ( in_eToValidate.SegmentDriver != d && d.CurrentTimeZoneMasterSegment == in_eToValidate) || 
                                                (in_eToValidate.SegmentDriver == d && d.CurrentTimeZoneMasterSegment == in_eToValidate.PrevSeg));

                // either we're the segmentdriver of the first segment, or else our mastersegment start time must be BEFORE our start.
                System.Diagnostics.Debug.Assert((in_eToValidate == FirstSeg && in_eToValidate.SegmentDriver == d && d.CurrentTimeZoneMasterSegment == null)
                                                ||  d.CurrentTimeZoneMasterSegment.TransitionStart <= d.TransitionStart);
            }
        }
        
        public void AddDriver(ITimeZoneDriver in_tz)
        {
            if (Destroyed)
            {
                System.Diagnostics.Debug.Assert(false);
                return;
            }

            if (AllDrivers.Contains(in_tz))
                return;

            AllDrivers.Add(in_tz);
        }


        public void RemoveDriver(ITimeZoneDriver in_tz)
        {
            if (Destroyed)
            {
                System.Diagnostics.Debug.Assert(false);
                return;
            }

            if (!AllDrivers.Contains(in_tz))
                return;

            AllDrivers.Remove(in_tz);
        }

        public bool DestroySeg()
        {
            TimeZoneObj.TimeZoneInfoUpdated -= new EventHandler(TimeZone_TimeZoneInfoUpdated);

            AllDrivers.Clear();
            Destroyed = true;

            return true;
        }

        public void ValidateStart()
        {
            g_tValidateTimer.Start();
        }
        /*
        public void CalcDriver()
        {
            if (Destroyed)
            {
                System.Diagnostics.Debug.Assert(false);
                throw new Exception();
            }

            DateTime dtStart = DateTime.MaxValue;
            TransitionStart = DateTime.MaxValue;
            foreach (ITimeZoneDriver tz in AllDrivers)
            {
                if (tz.Start <= dtStart)
                {

                    Start = dtStart = tz.Start;
                    TransitionStart = tz.TransitionStart;
                }
            }

            if (Start == DateTime.MaxValue)
            {
                Start = DateTime.MinValue;
                TransitionStart = DateTime.MinValue;
            }

            if (TransitionStart == DateTime.MaxValue)
                TransitionStart = Start;
        }*/

        #region ITimeZoneSegment Members

        

        #endregion


        #region ITimeZoneSlave Members

        public double CurrentOffset
        {
            get { throw new NotImplementedException(); }
        }

        public ITimeZoneSegment CurrentTimeZoneSegment
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public DateTime DockTime
        {
            get { throw new NotImplementedException(); }
        }

        #endregion
    }
}