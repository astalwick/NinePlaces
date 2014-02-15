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

namespace NinePlaces
{
    public interface ITimeZoneTickElement : ITickElement
    {
        new ITimeZoneSegment RepresentedSegment { get; set; }
    }

    public class TimeZoneTickElement : TickElement, ITimeZoneTickElement
	{
        public TimeZoneTickElement(ITickRibbon in_oParent, TickElementDuration in_td, ITimeZoneSegment in_TimeZone) : base( in_oParent, in_td )
		{
			// Required to initialize variables
			InitializeComponent();
            RepresentedSegment = in_TimeZone;
		}
        
        public override TickElementDuration TickDuration
        {
            get
            {
                return TickElementDuration.ArbitraryTimeSpan;
            }
        }

        public override void Disconnect()
        {
            RepresentedSegment = null;
            base.Disconnect();
        }

        
        #region ITimeZoneTickElement Members
        private ITimeZoneSegment m_tz = null;
        public override ITimeZoneSegment RepresentedSegment
        {
            get { return m_tz; }
            set 
            { 
                m_tz = value;
            }
        }

        public override DateTime TickStartTime
        {
            get
            {
                if (RepresentedSegment.Start == DateTime.MinValue || RepresentedSegment.Start < m_oParentRibbon.StartTime)
                {
                    return m_oParentRibbon.StartTime;
                }
                else
                {
                    return RepresentedSegment.Start;
                }
            }
            set
            {
                // no one sets TickStartTime.
            }
        }

        public override DateTime TickEndTime
        {
            get
            {
                if (RepresentedSegment.End == DateTime.MaxValue || RepresentedSegment.End > m_oParentRibbon.EndTime)
                {
                    return m_oParentRibbon.EndTime;
                }
                else
                {
                    return RepresentedSegment.End;
                }
            }
            set
            {
                // no one sets TickEndTime.
            }
        }

        #endregion
    }
}