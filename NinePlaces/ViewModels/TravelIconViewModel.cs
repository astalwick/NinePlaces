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
using NinePlaces.ViewModels;
using Common.Interfaces;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows.Data;
using Common;
using System.Globalization;
using NinePlaces.Undo;
using NinePlaces.Managers;

namespace NinePlaces.ViewModels
{

    public class TravelIconViewModel : DurationIconViewModel, IArrivalLocationProperties, IDepartureLocationProperties, INamedLocationProperties, ITimeZoneDriver
    {
        public TravelIconViewModel(IconTypes in_oIconType) 
            : base(in_oIconType) 
        {
            Arrival.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Arrival_PropertyChanged);
            Departure.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Departure_PropertyChanged);
        }

        public TravelIconViewModel(IconTypes in_oIconType, ITimelineElementModel in_oXMLModel) : base(in_oIconType, in_oXMLModel)
        {
            Arrival.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Arrival_PropertyChanged);
            Departure.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Departure_PropertyChanged);
        }

        public event EventHandler TimeZonePropertiesChanged;

        ILocationObject m_oArrivalObject = new LocationObject();
        public ILocationObject Arrival
        {
            get
            {
                return m_oArrivalObject;
            }
            protected set
            {
                m_oArrivalObject = value;
            }
        }
         
        ILocationObject m_oSelectedArrival = new LocationObject();
        //
        // SelectedArrival exists because the autocompletebox will give us an INCOMPLETE
        // location object.  It will give us things like city, province - but not street address.
        // We do not want to bind directly to the arrival object, because it will overwrite
        // streetaddress with empty strings.  So, we bind to selectedarrival, and then COPY
        // the new info over to arrival.
        //
        public ILocationObject SelectedArrival
        {
            get
            {
                return m_oSelectedArrival;
            }
            set
            {
                using (new AutoComplexUndo())
                {

                    m_oSelectedArrival = value;
                    if (m_oSelectedArrival != null)
                    {
                        Arrival.Copy(m_oSelectedArrival);
                        InvokeTimeZoneChanged();
                    }
                    else
                    {
                        m_oSelectedArrival = new LocationObject();
                        m_oSelectedArrival.Copy( Arrival );
                    }
                }
            }
        }


        private bool m_bSelectedDepartureExplicitlySet = false;
        protected ILocationObject m_oSelectedDeparture = new LocationObject();
        public ILocationObject SelectedDeparture
        {
            get
            {
                return m_oSelectedDeparture;
            }
            set
            {
                using (new AutoComplexUndo())
                {
                    if( value != null && !value.IsEmpty() )
                        m_bSelectedDepartureExplicitlySet = true;
                    m_oSelectedDeparture = value;
                    if (m_oSelectedDeparture != null)
                    {
                        Departure.Copy(m_oSelectedDeparture);
                    }
                    else
                    {
                        m_oSelectedDeparture = new LocationObject();
                        m_oSelectedDeparture.Copy(Departure);
                    }
                }
            }
        }

        private string m_strDefaultDepartureCity = string.Empty;
        public string DefaultDepartureCity
        {
            get
            {
                return m_strDefaultDepartureCity;
            }
            set
            {
                if (m_strDefaultDepartureCity != value)
                {
                    if (!m_bSelectedDepartureExplicitlySet)
                    {
                        SelectedDeparture.CityName = m_strDefaultDepartureCity = value;
                        NotifyPropertyChanged("DepartureCity");
                    }
                }
            }
        }

        void Arrival_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Name":
                    NotifyPropertyChanged("DestinationName", Arrival.Name);
                    break;
                case "CityName":
                    NotifyPropertyChanged("ArrivalCity", Arrival.CityName);
                    NotifyPropertyChanged("IconDescription");
                    break;
                case "Country":
                    NotifyPropertyChanged("ArrivalCountry", Arrival.Country);
                    break;
                case "Province":
                    NotifyPropertyChanged("ArrivalProvince", Arrival.Province);
                    break;
                case "PostalCode":
                    NotifyPropertyChanged("ArrivalPostalCode", Arrival.PostalCode);
                    break;
                case "StreetAddress":
                    NotifyPropertyChanged("ArrivalStreetAddress", Arrival.StreetAddress);
                    break;
                case "TimeZoneID":
                    TimeZoneID = Arrival.TimeZoneID;
                    NotifyPropertyChanged("TimeZone", Arrival.TimeZoneID);
                    InvokeTimeZoneChanged();
                    break;
            }
        }

        ILocationObject m_oDepartureObject = new LocationObject();
        public ILocationObject Departure
        {
            get
            {
                return m_oDepartureObject;
            }
            set
            {
                m_oDepartureObject = value;
            }
        }


        void Departure_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "CityName":
                    NotifyPropertyChanged("DepartureCity", Departure.CityName);
                    break;
                case "Country":
                    NotifyPropertyChanged("DepartureCountry", Departure.Country);
                    break;
                case "Province":
                    NotifyPropertyChanged("DepartureProvince", Departure.Province);
                    break;
                case "PostalCode":
                    NotifyPropertyChanged("DeparturePostalCode", Departure.PostalCode);
                    break;
                case "StreetAddress":
                    NotifyPropertyChanged("DepartureStreetAddress", Departure.StreetAddress);
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

        public DateTime DepartureTime
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

        public DateTime ArrivalTime
        {
            get
            {
                return EndTime;
            }
            set
            {
                EndTime = value;
            }
        }

        public DateTime Start
        {
            get
            {
                return DepartureTime + Duration;
            }
        }

        public DateTime TransitionStart
        {
            get
            {
                return DepartureTime;
            }
        }

        protected override string IconDescriptionDetails
        {
            get
            {
                return Arrival.CityName;
            }
        }

        public string DestinationName
        {
            get
            {
                return Arrival.CityName;
            }
            set
            {
                SelectedArrival.CityName = Arrival.CityName = value;
            }
        }

        public string DepartureCity
        {
            get
            {
                return string.IsNullOrEmpty(Departure.CityName) ? DefaultDepartureCity : Departure.CityName;
            }
            set
            {
                if (Departure.CityName != value)
                {
                    SelectedDeparture.CityName = Departure.CityName = value;
                    NotifyPropertyChanged("DepartureCity");
                }
            }
        }

        public string DepartureCountry
        {
            get
            {
                return Departure.Country;
            }
            set
            {
                SelectedDeparture.Country = Departure.Country = value;
            }
        }

        public string DepartureProvince
        {
            get
            {
                return Departure.Province;
            }
            set
            {
                SelectedDeparture.Province = Departure.Province = value;
            }
        }

        public string DeparturePostalCode
        {
            get
            {
                return Departure.PostalCode;
            }
            set
            {
                SelectedDeparture.PostalCode = Departure.PostalCode = value;
            }
        }

        public string DepartureStreetAddress
        {
            get
            {
                return Departure.StreetAddress;
            }
            set
            {
                SelectedDeparture.StreetAddress = Departure.StreetAddress = value;
            }
        }

        public string ArrivalCity
        {
            get
            {
                return Arrival.CityName;
            }
            set
            {
                SelectedArrival.CityName = Arrival.CityName = value;
            }
        }

        public string ArrivalCountry
        {
            get
            {
                return Arrival.Country;
            }
            set
            {
                SelectedArrival.Country = Arrival.Country = value;
            }
        }

        public string ArrivalProvince
        {
            get
            {
                return Arrival.Province;
            }
            set
            {
                SelectedArrival.Province = Arrival.Province = value;
            }
        }

        public string ArrivalPostalCode
        {
            get
            {
                return Arrival.PostalCode;
            }
            set
            {
                SelectedArrival.PostalCode = Arrival.PostalCode = value;
            }
        }

        public string ArrivalStreetAddress
        {
            get
            {
                return Arrival.StreetAddress;
            }
            set
            {
                SelectedArrival.StreetAddress = Arrival.StreetAddress = value;
            }
        }

        protected override bool UpdatePropertyFromModel(string in_strProperty)
        {
            using (new NoUndo())
            {
                if (Model == null)
                    return false;

                if (in_strProperty == "ArrivalCity" && Arrival.CityName != Model.GetProperty("ArrivalCity"))
                {
                    SelectedArrival.CityName = Arrival.CityName = Model.GetProperty("ArrivalCity");
                    return true;
                }

                if (in_strProperty == "TimeZone" && Arrival.TimeZoneID != Model.GetProperty("TimeZone"))
                {
                    TimeZoneID = SelectedArrival.TimeZoneID = Arrival.TimeZoneID = Model.GetProperty("TimeZone");
                    return true;
                }

                if (in_strProperty == "ArrivalStreetAddress" && Arrival.StreetAddress != Model.GetProperty("ArrivalStreetAddress"))
                {
                    SelectedArrival.StreetAddress = Arrival.StreetAddress = Model.GetProperty("ArrivalStreetAddress");
                    return true;
                }
                if (in_strProperty == "ArrivalCountry" && Arrival.Country != Model.GetProperty("ArrivalCountry"))
                {
                    SelectedArrival.Country = Arrival.Country = Model.GetProperty("ArrivalCountry");
                    return true;
                }

                if (in_strProperty == "ArrivalPostalCode" && Arrival.PostalCode != Model.GetProperty("ArrivalPostalCode"))
                {
                    SelectedArrival.PostalCode = Arrival.PostalCode = Model.GetProperty("ArrivalPostalCode");
                    return true;
                }

                if (in_strProperty == "ArrivalProvince" && Arrival.Province != Model.GetProperty("ArrivalProvince"))
                {
                    SelectedArrival.Province = Arrival.Province = Model.GetProperty("ArrivalProvince");
                    return true;
                }

                if (in_strProperty == "DepartureCity" && Departure.CityName != Model.GetProperty("DepartureCity"))
                {
                    SelectedDeparture.CityName = Departure.CityName = Model.GetProperty("DepartureCity");
                    return true;
                }

                if (in_strProperty == "DepartureStreetAddress" && Departure.StreetAddress != Model.GetProperty("DepartureStreetAddress"))
                {
                    SelectedDeparture.StreetAddress = Departure.StreetAddress = Model.GetProperty("DepartureStreetAddress");
                    return true;
                }

                if (in_strProperty == "DepartureCountry" && Departure.Country != Model.GetProperty("DepartureCountry"))
                {
                    SelectedDeparture.Country = Departure.Country = Model.GetProperty("DepartureCountry");
                    return true;
                }

                if (in_strProperty == "DeparturePostalCode" && Departure.PostalCode != Model.GetProperty("DeparturePostalCode"))
                {
                    SelectedDeparture.PostalCode = Departure.PostalCode = Model.GetProperty("DeparturePostalCode");
                    return true;
                }

                if (in_strProperty == "DepartureProvince" && Departure.Province != Model.GetProperty("DepartureProvince"))
                {
                    SelectedDeparture.Province = Departure.Province = Model.GetProperty("DepartureProvince");
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
        }

        protected override void UpdateBaseProperties()
        {
            if (Model == null)
                return;

            if (Model.HasProperty("TimeZone"))
            {
                TimeZoneID = SelectedArrival.TimeZoneID = Arrival.TimeZoneID = Model.GetProperty("TimeZone");
            }

            base.UpdateBaseProperties();
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
            if (TimeZonePropertiesChanged != null && (TimeZone==null || (!double.IsNaN(TimeZone.DSTOffset) && !double.IsNaN(TimeZone.Offset))))
                TimeZonePropertiesChanged.Invoke(this, new EventArgs());
        }


    }
}
