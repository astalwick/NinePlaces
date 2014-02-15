using Common.Interfaces;
using Common;
using System;
using System.Globalization;
using NinePlaces.Managers;
using NinePlaces.Undo;

namespace NinePlaces.ViewModels
{
    public class GenericIconViewModel : DurationIconViewModel, IMutableIconProperties, ILocationProperties, INamedActivityProperties, IMutableTimeZoneProperties, IArrivalLocationProperties
    {
        public event EventHandler CurrentClassChanged;
        public event EventHandler ExposesTimeZoneChanged;
        public event EventHandler TimeZonePropertiesChanged;
        public GenericIconViewModel(ITimelineElementModel in_oXMLModel)
            : base(IconTypes.GenericActivity, in_oXMLModel)
        {
            Location = new LocationObject();
            Location.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Location_PropertyChanged);
        }

        ILocationObject m_oLocation = new LocationObject();
        public ILocationObject Location
        {
            get
            {
                return m_oLocation;
            }
            set
            {
                m_oLocation = value;
            }
        }

        ILocationObject m_oSelectedLocation = new LocationObject();
        public ILocationObject SelectedLocation
        {
            get
            {
                return m_oSelectedLocation;
            }
            set
            {
                using (new AutoComplexUndo())
                {
                    m_oSelectedLocation = value;
                    if (m_oSelectedLocation != null)
                    {
                        Location.Copy(m_oSelectedLocation);
                        InvokeTimeZoneChanged();
                    }
                    else
                    {
                        m_oSelectedLocation = new LocationObject();
                        m_oSelectedLocation.Copy(Location);
                    }
                }
            }
        }

        void Location_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "CityName":
                    NotifyPropertyChanged("City", Location.CityName);
                    break;
                case "Country":
                    NotifyPropertyChanged("Country", Location.Country);
                    break;
                case "Province":
                    NotifyPropertyChanged("Province", Location.Province);
                    break;
                case "PostalCode":
                    NotifyPropertyChanged("PostalCode", Location.PostalCode);
                    break;
                case "StreetAddress":
                    NotifyPropertyChanged("StreetAddress", Location.StreetAddress);
                    break;
                case "TimeZoneID":
                    TimeZoneID = Location.TimeZoneID;
                    NotifyPropertyChanged("TimeZone", Location.TimeZoneID);
                    InvokeTimeZoneChanged();
                    break;
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
                if (DockTime != value)
                {
                    DateTime dtOrig = base.DockTime;
                    base.DockTime = value;

                    if (dtOrig != base.DockTime)
                        InvokeTimeZoneChanged();

                    NotifyPropertyChanged("LocalDockTime");
                }
            }
        }

        public override TimeSpan Duration
        {
            get
            {
                return base.Duration;
            }
            set
            {
                TimeSpan dtOrig = base.Duration;
                base.Duration = value;
                if (dtOrig != base.Duration)
                    InvokeTimeZoneChanged();
            }
        }

        public string LodgingName
        {
            get
            {
                return Location.Name;
            }
            set
            {
                SelectedLocation.Name = Location.Name = value;
            }
        }

        public string City
        {
            get
            {
                return Location.CityName;
            }
            set
            {
                SelectedLocation.CityName = Location.CityName = value;
            }
        }

        public string Country
        {
            get
            {
                return Location.Country;
            }
            set
            {
                SelectedLocation.Country = Location.Country = value;
            }
        }

        public string Province
        {
            get
            {
                return Location.Province;
            }
            set
            {
                SelectedLocation.Province = Location.Province = value;
            }
        }

        public string PostalCode
        {
            get
            {
                return Location.PostalCode;
            }
            set
            {
                SelectedLocation.PostalCode = Location.PostalCode = value;
            }
        }

        public string StreetAddress
        {
            get
            {
                return Location.StreetAddress;
            }
            set
            {
                SelectedLocation.StreetAddress = Location.StreetAddress = value;
            }
        }

        public DateTime Start
        {
            get
            {
                return DockTime + Duration;
            }
        }

        public DateTime TransitionStart
        {
            get
            {
                return DockTime;
            }
        }

        private IconClass m_oCurClass = IconClass.GenericActivity;
        public IconClass CurrentClass
        {
            get
            {
                return m_oCurClass;
            }
            set
            {
                if (m_oCurClass != value)
                {
                    bool bExposes = ExposesTimeZone;
                    m_oCurClass = value;
                    NotifyPropertyChanged("CurrentClass", m_oCurClass.ToString());
                    if( CurrentClassChanged != null )
                        CurrentClassChanged.Invoke(this, new EventArgs());

                    if( bExposes != ExposesTimeZone && ExposesTimeZoneChanged != null )
                        ExposesTimeZoneChanged.Invoke(this, new EventArgs());
                }
            }
        }

        public bool ExposesTimeZone
        {
            get
            {
                return m_oCurClass == IconClass.Transportation;
            }
        }

        private string m_strGenericName = "";
        public string ActivityName
        {
            get
            {
                return m_strGenericName;
            }
            set
            {
                m_strGenericName = value;
                IconDescriptionDetails = m_strGenericName;
                NotifyPropertyChanged("ActivityName", m_strGenericName);
            }
        }

        protected override void UpdateBaseProperties()
        {
            if (Model == null)
                return;

            if (Model.HasProperty("CurrentClass"))
            {
                string strClass = Model.GetProperty("CurrentClass");
                m_oCurClass = IconRegistry.StringToIconClass(strClass);
            }


            if (Model.HasProperty("TimeZone"))
            {
                TimeZoneID = SelectedLocation.TimeZoneID = Location.TimeZoneID = Model.GetProperty("TimeZone");
            }

            base.UpdateBaseProperties();
        }


        protected override bool UpdatePropertyFromModel(string in_strProperty)
        {
            if (Model == null)
                return false;

            if (in_strProperty == "LodgingName" && Location.Name != Model.GetProperty("LodgingName"))
            {
                SelectedLocation.Name = Location.Name = Model.GetProperty("LodgingName");
                return true;
            }

            if (in_strProperty == "City" && Location.CityName != Model.GetProperty("City"))
            {
                SelectedLocation.CityName = Location.CityName = Model.GetProperty("City");
                return true;
            }

            if (in_strProperty == "StreetAddress" && Location.StreetAddress != Model.GetProperty("StreetAddress"))
            {
                SelectedLocation.StreetAddress = Location.StreetAddress = Model.GetProperty("StreetAddress");
                return true;
            }

            if (in_strProperty == "Country" && Location.Country != Model.GetProperty("Country"))
            {
                SelectedLocation.Country = Location.Country = Model.GetProperty("Country");
                return true;
            }

            if (in_strProperty == "PostalCode" && Location.PostalCode != Model.GetProperty("PostalCode"))
            {
                SelectedLocation.PostalCode = Location.PostalCode = Model.GetProperty("PostalCode");
                return true;
            }

            if (in_strProperty == "Province" && Location.Province != Model.GetProperty("Province"))
            {
                SelectedLocation.Province = Location.Province = Model.GetProperty("Province");
                return true;
            }

            if (in_strProperty == "ActivityName" && m_strGenericName != Model.GetProperty("ActivityName"))
            {
                m_strGenericName = Model.GetProperty("ActivityName");
                IconDescriptionDetails = m_strGenericName;
                return true;
            }


            if (in_strProperty == "CurrentClass" && m_oCurClass.ToString() != Model.GetProperty("CurrentClass"))
            {
                string strClass = Model.GetProperty("CurrentClass");
                m_oCurClass = IconRegistry.StringToIconClass(strClass);

                return true;
            }
            if (in_strProperty == "TimeZone" && Location.TimeZoneID != Model.GetProperty("TimeZone"))
            {
                TimeZoneID = SelectedLocation.TimeZoneID = Location.TimeZoneID = Model.GetProperty("TimeZone");
                return true;
            }


            bool bMustInvoke = false;
            if (in_strProperty == "DockTime")
            {
                DateTime dtTime;
                DateTime.TryParseExact(Model.GetProperty("DockTime"), "yyyy-MM-ddTHH:mm:ss.ff", CultureInfo.InvariantCulture, DateTimeStyles.None, out dtTime);

                if (dtTime != DockTime)
                {
                    bMustInvoke = true;
                }
                // no return here.  base class needs to handle this as well.
            }

            bool bRes = base.UpdatePropertyFromModel(in_strProperty);
            if (bMustInvoke)
                InvokeTimeZoneChanged();
            return bRes;
        }

        protected ITimeZone m_tz = null;
        public ITimeZone TimeZone
        {
            get
            {
                return m_tz;
            }
            set
            {
                m_tz = value;
            }
        } 

        public double TimeZoneOffset
        {
            get { return TimeZone.Offset; }
        }

        public double TimeZoneDSTOffset
        {
            get { return TimeZone.DSTOffset; }
        }

        public string TimeZoneID
        {
            get { return TimeZone == null ? string.Empty : TimeZone.TimeZoneID; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    TimeZone = null;
                    NotifyTimeZoneInfoUpdated();
                }
                else
                {
                    TimeZone = TimeZoneManager.GetTimeZone(value);
                    if (!TimeZone.Loaded)
                        TimeZone.TimeZoneInfoUpdated += new EventHandler(TimeZone_TimeZoneInfoUpdated);
                    else
                        NotifyTimeZoneInfoUpdated();
                }
            }
        }

        private void NotifyTimeZoneInfoUpdated()
        {
            InvokeTimeZoneChanged();
            dEndOffset = double.MinValue;
            NotifyPropertyChanged("LocalEndTime");
        }

        void TimeZone_TimeZoneInfoUpdated(object sender, EventArgs e)
        {
            NotifyTimeZoneInfoUpdated();
        }

        protected void InvokeTimeZoneChanged()
        {
            if (TimeZonePropertiesChanged != null && (TimeZone == null || (!double.IsNaN(TimeZone.DSTOffset) && !double.IsNaN(TimeZone.Offset))))
                TimeZonePropertiesChanged.Invoke(this, new EventArgs());
        }



        #region IArrivalLocationProperties Members

        public string ArrivalCity
        {
            get
            {
                return City;
            }
            set
            {
                City = value;
            }
        }

        public string ArrivalCountry
        {
            get
            {
                return Country;
            }
            set
            {
                Country = value;
            }
        }

        public string ArrivalProvince
        {
            get
            {
                return Province;
            }
            set
            {
                Province = value;
            }
        }

        public string ArrivalPostalCode
        {
            get
            {
                return StreetAddress;
            }
            set
            {
                PostalCode = value;
            }
        }

        public string ArrivalStreetAddress
        {
            get
            {
                return StreetAddress;
            }
            set
            {
                StreetAddress = value;
            }
        }

        public DateTime ArrivalTime
        {
            get
            {
                return DockTime;
            }
            set
            {
                DockTime = value;
            }
        }

        #endregion
    }
}
