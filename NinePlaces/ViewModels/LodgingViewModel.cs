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
using Common;

namespace NinePlaces.ViewModels
{
    public class LodgingViewModel : DurationIconViewModel, ILocationProperties, INamedLodgingProperties
    {
        public LodgingViewModel(IconTypes in_oIconType, ITimelineElementModel in_oXMLModel)
            : base(in_oIconType, in_oXMLModel)
        {
            Location = new LocationObject();
            Location.PropertyChanged +=new System.ComponentModel.PropertyChangedEventHandler(Location_PropertyChanged);
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

        void Location_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Name":
                    NotifyPropertyChanged("LodgingName", Location.Name);
                    NotifyPropertyChanged("IconDescription");
                    break;
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

        protected override string IconDescriptionDetails
        {
            get
            {
                return Location.Name;
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
                Location.Name = value;
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

        protected override bool UpdatePropertyFromModel(string in_strProperty)
        {
            if (Model == null)
                return false;

            if (in_strProperty == "LodgingName" && Location.Name != Model.GetProperty("LodgingName"))
            {
                Location.Name = Model.GetProperty("LodgingName");
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
