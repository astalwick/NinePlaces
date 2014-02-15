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
using Common;

namespace NinePlaces.ViewModels
{
    public class ActivityIconViewModel : IconViewModel, INamedActivityProperties, INamedLocationProperties, ILocationProperties
    {
        public ActivityIconViewModel(IconTypes in_icType, ITimelineElementModel in_oXMLModel)
            : base(in_icType, in_oXMLModel)
        {
            Location = new LocationObject();
            Location.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Location_PropertyChanged);
        }


        ILocationObject m_oLocation = null;
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

        ILocationObject m_oSelectedLocation = null;
        public ILocationObject SelectedLocation
        {
            get
            {
                return m_oSelectedLocation;
            }
            set
            {
                m_oSelectedLocation = value;
                if (m_oSelectedLocation != null)
                {
                    Location.Copy(m_oSelectedLocation);
                }
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
                Location.CityName = value;
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
                Location.Country = value;
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
                Location.Province = value;
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
                Location.PostalCode = value;
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
                Location.StreetAddress = value;
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
            }
        }
        private string m_strActivityName = "";
        public string ActivityName
        {
            get
            {
                return m_strActivityName;
            }
            set
            {
                m_strActivityName = value;
                IconDescriptionDetails = m_strActivityName;
                NotifyPropertyChanged("ActivityName", m_strActivityName);
            }
        }

        private string m_strLocation = "";
        public string DestinationName
        {
            get
            {
                return m_strLocation;
            }
            set
            {
                m_strLocation = value;
                IconDescriptionDetails = m_strLocation;
                NotifyPropertyChanged("DestinationName", m_strLocation);
            }
        }

        protected override bool UpdatePropertyFromModel(string in_strProperty)
        {
            if (Model == null)
                return false;

            if (in_strProperty == "ActivityName" && m_strActivityName != Model.GetProperty("ActivityName"))
            {
                m_strActivityName = Model.GetProperty("ActivityName");
                IconDescriptionDetails = m_strActivityName;
                return true;
            }

            if (in_strProperty == "DestinationName" && m_strLocation != Model.GetProperty("DestinationName"))
            {
                m_strLocation = Model.GetProperty("DestinationName");
                IconDescriptionDetails = m_strLocation;
                return true;
            }
            
            if (in_strProperty == "City" && Location.CityName != Model.GetProperty("City"))
            {
                Location.CityName = Model.GetProperty("City");
                return true;
            }

            if (in_strProperty == "StreetAddress" && Location.StreetAddress != Model.GetProperty("StreetAddress"))
            {
                Location.StreetAddress = Model.GetProperty("StreetAddress");
                return true;
            }

            if (in_strProperty == "Country" && Location.Country != Model.GetProperty("Country"))
            {
                Location.Country = Model.GetProperty("Country");

                return true;
            }

            if (in_strProperty == "PostalCode" && Location.PostalCode != Model.GetProperty("PostalCode"))
            {
                Location.PostalCode = Model.GetProperty("PostalCode");
                return true;
            }

            if (in_strProperty == "Province" && Location.Province != Model.GetProperty("Province"))
            {
                Location.Province = Model.GetProperty("Province");
                return true;
            }

            return base.UpdatePropertyFromModel(in_strProperty);
        }

    }
}
