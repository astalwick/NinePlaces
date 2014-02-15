//#define StickyEndTime

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
using NinePlaces.Helpers;


namespace NinePlaces.ViewModels
{

    public class DurationIconViewModel : IconViewModel, IDurationIconViewModel
    {
        public DurationIconViewModel(IconTypes in_oIconType)
            : base(in_oIconType)
        {
        }

        public DurationIconViewModel(IconTypes in_oIconType, ITimelineElementModel in_oXMLModel)
            : base(in_oIconType, in_oXMLModel)
        {
        }

        private bool m_bDurationEnabled = true;
        public bool DurationEnabled
        {
            get
            {
                return m_bDurationEnabled;
            }
            set
            {
                if (m_bDurationEnabled != value)
                    m_bDurationEnabled = value;
            }
        }

        private TimeSpan m_tsDefaultDuration = new TimeSpan(6,0,0);
        public TimeSpan DefaultDuration
        {
            get
            {
                return m_tsDefaultDuration;
            }
            protected set
            {
                if (m_tsDefaultDuration != value)
                    m_tsDefaultDuration = value;
            }
        }


        private bool m_bEndTimeExplicitlySet = false;

        private TimeSpan m_tsDuration = new TimeSpan(6, 0, 0);
        public virtual TimeSpan Duration
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
                    if (m_tsDuration.Ticks < 0)
                    {
                        //
                        // our duration has gone negative (our startdate is after
                        // our enddate).  reset the duration back to its implicit setting.
                        // the user loses their 'explicitly set' end time.
                        //
                        m_bEndTimeExplicitlySet = false;
                        m_tsDuration = DefaultDuration;
                    }
                    else
                    {
                        // the duration has changed - we're no longer using our 
                        // default (implicitly set) duration.  that means the end time has been
                        // explicitly set at some point.
                        m_bEndTimeExplicitlySet = true;
                    }

                    NotifyPropertyChanged("Duration", m_tsDuration.ToString());
                    dEndOffset = App.TimeZones.OffsetAtTime(EndTime);
                }
            }
        }


        private bool m_bStickyEndTime = false;
        public bool StickyEndTime
        {
            get
            {
#if StickyEndTime
                return m_bEndTimeExplicitlySet || m_bStickyEndTime;
#else
                return false;
#endif

            }
            set
            {
                m_bStickyEndTime = value;
            }
        }

        public override DateTime DockTime
        {
            get
            {
                return base.DockTime;
            }
            set
            {
                if (base.DockTime != value)
                {
                    base.DockTime = value;
                    if (StickyEndTime)
                    {
                        // the end time is sticky!  the user has explicitly set an
                        // end time, so we will always update the duration so that
                        // it matches the end time that the user expects.
                        Duration = EndTime - value;
                    }
                    else
                    {
                        // the end time has not been set by the user.  that means that
                        // we're using the implicitly set duration to calculate the end time.
                        // that will happen in the EndTime property.  However, we notify
                        // here so that the world knows that the endtime has changed *in 
                        // addition to the docktime*
                        
                        NotifyPropertyChanged("EndTime");
                        dEndOffset = App.TimeZones.OffsetAtTime(EndTime);
                        NotifyPropertyChanged("LocalEndTime");
                    }
                }
            }
        }

        public virtual DateTime EndTime
        {
            get
            {
                return DockTime + m_tsDuration;
            }
            set
            {

                // setting the EndTime property updates the duration.

                // IF the new endtime is earlier than the docktime, 
                // then we snap the endtime to the docktime.
                if (value < DockTime)
                {
                    value = DockTime;
                    Duration = value - DockTime;
                    m_bEndTimeExplicitlySet = false;
                    NotifyPropertyChanged("EndTime");
                    dEndOffset = App.TimeZones.OffsetAtTime(EndTime);
                    NotifyPropertyChanged("LocalEndTime");
                    return;
                }

                Duration = value - DockTime;
                System.Diagnostics.Debug.Assert(m_bEndTimeExplicitlySet);
                NotifyPropertyChanged("EndTime");
                dEndOffset = App.TimeZones.OffsetAtTime(EndTime);
                NotifyPropertyChanged("LocalEndTime");
            }
        }

        protected double dEndOffset = double.MinValue;
        public virtual DateTime LocalEndTime
        {
            get
            {
                if( dEndOffset == double.MinValue )
                    dEndOffset = App.TimeZones.OffsetAtTime(EndTime);

                return EndTime.AddHours(dEndOffset);
            }
            set
            {
                dEndOffset = App.TimeZones.OffsetAtTime(EndTime);
                EndTime = value.AddHours(-dEndOffset);
            }
        }

        protected override bool UpdatePropertyFromModel(string in_strProperty)
        {
            if (Model == null)
                return false;

            if (in_strProperty == "Duration")
            {
                TimeSpan tsDur;
                TimeSpan.TryParse(Model.GetProperty("Duration"), out tsDur);
                if (tsDur != m_tsDuration)
                {
                    // the duration property was set in the model - that means that
                    // the user explicitly set the end time.  we need to update the end
                    // time to reflect it.
                    m_bEndTimeExplicitlySet = true;
                    m_tsDuration = tsDur;
                    return true;
                }
            }

            return base.UpdatePropertyFromModel(in_strProperty);
        }

        protected override void UpdateBaseProperties()
        {
            if (Model == null)
                return;

            if (Model.HasProperty("Duration"))
            {
                TimeSpan tsDur;
                TimeSpan.TryParse(Model.GetProperty("Duration"), out tsDur);
                if (tsDur != m_tsDuration)
                {
                    // the duration property was set in the model - that means that
                    // the user explicitly set the end time.  we need to update the end
                    // time to reflect it.
                    m_bEndTimeExplicitlySet = true;
                    m_tsDuration = tsDur;
                }
            }

            base.UpdateBaseProperties();
        }
    }
}
