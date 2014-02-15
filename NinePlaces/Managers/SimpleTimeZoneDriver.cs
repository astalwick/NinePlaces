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
    public class SimpleTimeZoneDriver : ITimeZoneDriver
    {
        private string m_tzID = string.Empty;
        public string TimeZoneID
        {
            get { return TimeZone == null ? string.Empty : TimeZone.TimeZoneID; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    TimeZone = null;
                }
                else
                {
                    TimeZone = TimeZoneManager.GetTimeZone(value);
                    if (!TimeZone.Loaded)
                        TimeZone.TimeZoneInfoUpdated += new EventHandler(TimeZone_TimeZoneInfoUpdated);
                    else
                        InvokeTimeZoneChanged();
                }
            }
        }

        protected void InvokeTimeZoneChanged()
        {
            if (TimeZonePropertiesChanged != null && !double.IsNaN(TimeZone.DSTOffset) && !double.IsNaN(TimeZone.Offset))
                TimeZonePropertiesChanged.Invoke(this, new EventArgs());
        }

        void TimeZone_TimeZoneInfoUpdated(object sender, EventArgs e)
        {
            InvokeTimeZoneChanged();
        }

        private ITimeZone m_tz = null;
        public ITimeZone TimeZone
        {
            get { return m_tz; }
            internal set { m_tz = value; m_tzID = m_tz.TimeZoneID; }
        }

        private DateTime m_dtTransitionStart = DateTime.MinValue;
        public DateTime TransitionStart
        {
            get { return m_dtTransitionStart; }
            set { m_dtTransitionStart = value; InvokeTimeZoneChanged(); }
        }

        private DateTime m_dtStart = DateTime.MinValue;
        public DateTime Start
        {
            get { return m_dtStart; }
            set { m_dtStart = value; InvokeTimeZoneChanged(); }
        }

        public event EventHandler TimeZonePropertiesChanged;

        public double CurrentOffset
        {
            get { return m_oCurrentSegment.TimeZoneObj.OffsetAtTime(DockTime); }
        }

        private ITimeZoneSegment m_oCurrentSegment = null;
        public ITimeZoneSegment CurrentTimeZoneMasterSegment
        {
            get
            {
                return m_oCurrentSegment;
            }
            set
            {
                if (m_oCurrentSegment != value)
                    m_oCurrentSegment = value;
            }
        }
        
        public DateTime DockTime
        {
            get { return TransitionStart; }
        }
    }
}