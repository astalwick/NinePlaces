using System;
using Common.Interfaces;
using Common;
using NinePlaces.Undo;
using NinePlaces.Helpers;
using System.Globalization;
using System.Windows.Threading;

namespace NinePlaces.ViewModels
{
    public abstract class IconViewModel : BaseViewModel, IIconViewModel, IBasicProperties, ITimeZoneSlave
    {
        public IconViewModel(IconTypes in_oIconType) : base()
        {
            IconType = in_oIconType;
            // the BASE class for every icon will register to get notified of localization changes.
            // if the culture has changed, we must update the icon description.
            Common.CrossAssemblyNotifyContainer.LocalizationChange += new EventHandler(CrossAssemblyNotifyContainer_LocalizationChange);

            m_oZoneAdjustTimer.Interval = new TimeSpan(0, 0, 0, 0, 125);
            m_oZoneAdjustTimer.Tick += new EventHandler(m_oZoneAdjustTimer_Tick);
            m_oLocalDateTimeTimer.Interval = new TimeSpan(0, 0, 0, 0, 700);
            m_oLocalDateTimeTimer.Tick += new EventHandler(m_oLocalDateTimeTimer_Tick);
        }




        public IconViewModel(IconTypes in_oIconType, ITimelineElementModel in_oXMLModel)
            : base()
        {
            IconType = in_oIconType;
            Model = in_oXMLModel;
            // the BASE class for every icon will register to get notified of localization changes.
            // if the culture has changed, we must update the icon description.
            Common.CrossAssemblyNotifyContainer.LocalizationChange += new EventHandler(CrossAssemblyNotifyContainer_LocalizationChange);

            m_oZoneAdjustTimer.Interval = new TimeSpan(0, 0, 0, 0, 25);
            m_oZoneAdjustTimer.Tick +=new EventHandler(m_oZoneAdjustTimer_Tick);
            m_oLocalDateTimeTimer.Interval = new TimeSpan(0, 0, 0, 0, 700);
            m_oLocalDateTimeTimer.Tick += new EventHandler(m_oLocalDateTimeTimer_Tick);
        }

        public virtual IVacationViewModel VacationVM
        {
            get
            {
                return ParentViewModel as IVacationViewModel;
            }
        }

        public override ITimelineElementModel Model
        {
            get
            {
                return base.Model;
            }
            internal set
            {
                base.Model = value;
            }
        }

        protected virtual void CrossAssemblyNotifyContainer_LocalizationChange(object sender, EventArgs e)
        {
            /// we've changed languages - update the description to reflect the new language.
            UpdateIconDescription();
        }

        protected override void Model_LoadError(object sender, EventArgs e)
        {
            Log.WriteLine("IconViewModel received a LoadError", LogEventSeverity.CriticalError);
            base.Model_LoadError(sender, e);
        }

        protected override void Model_SaveError(object sender, EventArgs e)
        {
            Log.WriteLine("IconViewModel received a SaveError", LogEventSeverity.CriticalError);
            base.Model_SaveError(sender, e);
        }

        // need the child type!
        IconTypes m_oIconType = IconTypes.Undefined;
        public IconTypes IconType
        {
            get
            {
                return m_oIconType;
            }
            internal set
            {
                m_oIconType = value;
                NotifyPropertyChanged("IconType", m_oIconType.ToString());
            }
        }

        private IIcon m_oIcon = null;
        public new IInterfaceElementWithVM InterfaceElement
        {
            get 
            { 
                return m_oIcon as IInterfaceElementWithVM;
            }
            set 
            {
                if (value == null)
                {
                    m_oIcon = null;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(value is IIcon);
                    m_oIcon = value as IIcon;
                }
            }
        }

        private string m_strNotes = "";
        public string Notes
        {
            get
            {
                return m_strNotes;
            }
            set
            {
                m_strNotes = value;
                NotifyPropertyChanged("Notes", m_strNotes);
            }
        }

        // docktime property is important for docking!
        private DateTime m_dtDockTime;
        public virtual DateTime DockTime
        {
            get
            {
                return m_dtDockTime;
            }
            set
            {
                if (m_dtDockTime != value)
                {
                    m_dtDockTime = value;
                    if (m_dtDockTime.Ticks == 0)
                        System.Diagnostics.Debug.Assert(false);
                    NotifyPropertyChanged("DockTime", m_dtDockTime.ToPersistableString());
                    NotifyPropertyChanged("LocalDockTime");
                }
            }
        }

        private bool m_bLocalDockTimeUnreachable = false;
        public virtual bool LocalDockTimeUnreachable
        {
            get
            {
                return m_bLocalDockTimeUnreachable;
            }
            set
            {
                if (m_bLocalDockTimeUnreachable != value)
                {
                    m_bLocalDockTimeUnreachable = value;
                    if (m_bLocalDockTimeUnreachable)
                    {
                        m_oLocalDateTimeTimer.Start();
                    }
                    else
                    {
                        // we notify immediately if we set it to false.
                        // if we set it to true, however, we don't notify
                        // until a certain amount of time goes by.
                        m_oLocalDateTimeTimer.Stop();
                        NotifyPropertyChanged("LocalDockTimeUnreachable");
                    }
                }
            }
        }

        void m_oLocalDateTimeTimer_Tick(object sender, EventArgs e)
        {
            m_oLocalDateTimeTimer.Stop();
            NotifyPropertyChanged("LocalDockTimeUnreachable");
        }

        public virtual DateTime LocalDockTime
        {
            get
            {
                return DockTime.AddHours(CurrentOffset);
            }
            set
            {
                DockTime = value.AddHours(-CurrentOffset);
            }
        }

        private string m_strDescriptionDetails = string.Empty;
        protected virtual string IconDescriptionDetails
        { 
            get
            {
                return m_strDescriptionDetails;
            }
            set
            {
                m_strDescriptionDetails = value;
                NotifyPropertyChanged("IconDescription");
            }
        }

        // icondescription helps with hover text!
        public string IconDescription
        {
            get
            {
                if( string.IsNullOrEmpty( IconDescriptionDetails ) )
                    return App.Resource.GetString(IconType.ToString() + "HoverText");
                else
                    return String.Format(App.Resource.GetString(IconType.ToString() + "HoverTextDetails"), IconDescriptionDetails, StringComparison.InvariantCulture);
            }
        }

        // this does nothing in the base.  does useful stuff in the children.
        protected virtual void UpdateIconDescription()
        {
            NotifyPropertyChanged("IconDescription");
        }

        public override void NotifyPropertyChanged(string in_strPropertyName, string in_strPersistablePropertyValue)
        {
            using (new AutoComplexUndo())
            {
                base.NotifyPropertyChanged(in_strPropertyName, in_strPersistablePropertyValue);

                // we override this so that we can update the icondescription if it's a helpful thing to do...
                if (in_strPropertyName != "IconDescription")
                    UpdateIconDescription();
            }
        }

        protected override bool UpdatePropertyFromModel(string in_strProperty)
        {
            if (Model == null)
                return false;

            if (in_strProperty == "DockTime" && Model.HasProperty( "DockTime" ))
            {
                DateTime dtTime;
                DateTime.TryParseExact(Model.GetProperty("DockTime"), "yyyy-MM-ddTHH:mm:ss.ff", CultureInfo.InvariantCulture, DateTimeStyles.None, out dtTime);
                
                if (dtTime != m_dtDockTime)
                {
                    m_dLastOffset = Double.MinValue;
                    m_dtDockTime = dtTime;
                    System.Diagnostics.Debug.Assert(m_dtDockTime.Ticks > 0);

                    UpdateIconDescription();

                    return true;
                }
            }

            if (in_strProperty == "Notes" && m_strNotes != Model.GetProperty("Notes"))
            {
                m_strNotes = Model.GetProperty("Notes");
                return true;
            }

            return base.UpdatePropertyFromModel(in_strProperty) ;
        }

        protected override void UpdateBaseProperties()
        {
            if (Model == null)
                return;

            try
            {
                if (Model.HasProperty("DockTime"))
                {
                    DateTime.TryParseExact(Model.GetProperty("DockTime"), "yyyy-MM-ddTHH:mm:ss.ff", CultureInfo.InvariantCulture, DateTimeStyles.None, out m_dtDockTime);
                    System.Diagnostics.Debug.Assert(m_dtDockTime.Ticks > 0);
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                System.Diagnostics.Debug.Assert(false);
            }

            base.UpdateBaseProperties();
        }

        protected override void Model_LoadComplete(object sender, EventArgs e)
        {
            base.Model_LoadComplete(sender, e);

            if (Model == null)
                return;

            InvokeLoadComplete(this, new EventArgs());
            InvokeChildrenLoaded(this, new EventArgs());
        }

        #region ITimeZoneSlave Members
        public double CurrentOffset
        {
            get
            {
                if (CurrentTimeZoneMasterSegment == null)
                    return 0;

                return CurrentTimeZoneMasterSegment.TimeZoneObj.OffsetAtTime(DockTime, false);
            }
        }

        DateTime m_dtLocalTime = DateTime.MinValue;
        protected ITimeZoneSegment m_oCurrentTimeZone = null;
        public virtual ITimeZoneSegment CurrentTimeZoneMasterSegment
        {
            get
            {
                return m_oCurrentTimeZone;
            }
            set
            {
                if (m_oCurrentTimeZone != value)
                {
                    if (m_oCurrentTimeZone != null)
                        m_oCurrentTimeZone.TimeZoneSegmentChanged -= new SegmentChangedEventHandler(m_oCurrentTimeZone_TimeZoneSegmentChanged);
                    
                    m_oCurrentTimeZone = value;
                    m_oZoneAdjustTimer.Start();
                    
                    if (m_oCurrentTimeZone != null)
                        m_oCurrentTimeZone.TimeZoneSegmentChanged += new SegmentChangedEventHandler(m_oCurrentTimeZone_TimeZoneSegmentChanged);
                }
            }
        }

        protected virtual void m_oCurrentTimeZone_TimeZoneSegmentChanged(object sender, SegmentChangedEventArgs args)
        {
            m_oZoneAdjustTimer.Start();
        }

        protected DispatcherTimer m_oZoneAdjustTimer = new DispatcherTimer();
        protected DispatcherTimer m_oLocalDateTimeTimer = new DispatcherTimer();
        protected bool m_bDeferredAdjust = false;
        protected double m_dLastOffset = double.MinValue;
        protected virtual bool AdjustDockTime()
        {
            m_oZoneAdjustTimer.Stop();

            lock (m_oZoneAdjustTimer)
            {
                //return true ;

                if (CurrentTimeZoneMasterSegment == null || !CurrentTimeZoneMasterSegment.TimeZoneObj.Loaded)
                    return false;

                double dNewOffset = CurrentOffset;
                if (m_dLastOffset == double.MinValue)
                {
                    m_dLastOffset = CurrentOffset;
                    LocalDockTimeUnreachable = false;

                    return true;
                }

                if (m_dLastOffset == dNewOffset)
                {
                    LocalDockTimeUnreachable = false;

                    return false;
                }

                DateTime DesiredLocalTime = DockTime.AddHours(m_dLastOffset);
                DateTime AdjustedDockTime = DockTime.AddHours(m_dLastOffset - dNewOffset);
                

                if (this is ITimeZoneDriver && CurrentTimeZoneMasterSegment.NextSeg != null && CurrentTimeZoneMasterSegment.NextSeg.SegmentDriver == this )
                {
                    // we are a segment driver.
                    // this is a slightly different case... we need to compare against our NEXTSEG end.

                    if( AdjustedDockTime < CurrentTimeZoneMasterSegment.TransitionStart ||
                        AdjustedDockTime > CurrentTimeZoneMasterSegment.NextSeg.End) 
                    {
                        LocalDockTimeUnreachable = true;

                        m_oZoneAdjustTimer.Start();
                        return false;
                    }

                }
                else if (AdjustedDockTime < CurrentTimeZoneMasterSegment.TransitionStart ||
                    AdjustedDockTime > CurrentTimeZoneMasterSegment.End)
                {
                    // this adjustment would cause problems.
                    // this item would jump in FRONT of its current master. 
                    // that would cause the current master to slave to this segment.

                    LocalDockTimeUnreachable = true;
                    
                    m_oZoneAdjustTimer.Start();
                    return false;
                }

                LocalDockTimeUnreachable = false;

                double dOld = m_dLastOffset;
                m_dLastOffset = dNewOffset;

                using (new NoUndo())
                {
                    DockTime = DockTime.AddHours(dOld - dNewOffset);
                }
            }
            return true;
        }

        void m_oZoneAdjustTimer_Tick(object sender, EventArgs e)
        {
            if (AdjustDockTime())
            {
                LocalDockTimeUnreachable = false;
                NotifyPropertyChanged("LocalDockTime");
            }
        }

        #endregion
    }
}
