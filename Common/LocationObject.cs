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
using System.ComponentModel;
using System.Windows.Data;

namespace Common
{


    public class LocationObject : INotifyPropertyChanged, ILocationObject
    {
        public void Copy(ILocationObject in_oObject)
        {

            // note: this is a bit of a hack.
            // street, postalcode and name are NOT SET by incoming timezone location
            // objects.  the rest are.  SO: if a user has entered a streetaddress,
            // and then selects a city - well, lets not overwrite it. 
            if (!string.IsNullOrEmpty(in_oObject.StreetAddress))
                StreetAddress = in_oObject.StreetAddress;
            if (!string.IsNullOrEmpty(in_oObject.PostalCode))
                PostalCode = in_oObject.PostalCode;
            if (!string.IsNullOrEmpty(in_oObject.Name))
                Name = in_oObject.Name;

            //if (!string.IsNullOrEmpty(in_oObject.CityName))
            CityName = in_oObject.CityName;
            //if (!string.IsNullOrEmpty(in_oObject.Country))
            Country = in_oObject.Country;
            //if (!string.IsNullOrEmpty(in_oObject.Province))
            Province = in_oObject.Province;
            //if (!string.IsNullOrEmpty(in_oObject.TimeZoneID))
            TimeZoneID = in_oObject.TimeZoneID;
            
        }
        private string m_strCityName = string.Empty;
        public string CityName 
        { 
            get
            {
                return m_strCityName;
            }
            set
            {
                if (m_strCityName != value)
                {
                    m_strCityName = value;
                    NotifyPropertyChanged("CityName");
                }
            }
        }

        private string m_strCountry = string.Empty;
        public string Country 
        { 
            get
            {
                return m_strCountry;
            }
            set
            {
                if (m_strCountry != value)
                {
                    m_strCountry = value;
                    NotifyPropertyChanged("Country");
                }
            }
        }
        
        private string m_strProvince = string.Empty;
        public string Province 
        { 
            get
            {
                return m_strProvince;
            }
            set
            {
                if (m_strProvince != value)
                {
                    m_strProvince = value;
                    NotifyPropertyChanged("Province");
                }
            }
        }

        private string m_strTimeZoneID = string.Empty;
        public string TimeZoneID 
        { 
            get
            {
                return m_strTimeZoneID;
            }
            set
            {
                if (m_strTimeZoneID != value)
                {
                    m_strTimeZoneID = value;
                    NotifyPropertyChanged("TimeZoneID");
                }
            }
        }
                
        private string m_strStreetAddress = string.Empty;
        public string StreetAddress 
        { 
            get
            {
                return m_strStreetAddress;
            }
            set
            {
                if (m_strStreetAddress != value)
                {
                    m_strStreetAddress = value;
                    NotifyPropertyChanged("StreetAddress");
                }
            }
        }

        private string m_strPostalCode = string.Empty;
        public string PostalCode 
        { 
            get
            {
                return m_strPostalCode;
            }
            set
            {
                if (m_strPostalCode != value)
                {
                    m_strPostalCode = value;
                    NotifyPropertyChanged("PostalCode");
                }
            }
        }

        private string m_strName = string.Empty;
        public string Name 
        { 
            get
            {
                return m_strName;
            }
            set
            {
                if (m_strName != value)
                {
                    m_strName = value;
                    NotifyPropertyChanged("Name");
                }
            }
        }

        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(CityName) &&
                string.IsNullOrEmpty(Country) &&
                string.IsNullOrEmpty(Province) &&
                string.IsNullOrEmpty(TimeZoneID) &&
                string.IsNullOrEmpty(StreetAddress) &&
                string.IsNullOrEmpty(PostalCode) &&
                string.IsNullOrEmpty(Name);

        }

        public LocationObject()
        {
            CityName = string.Empty;
            Country = string.Empty;
            Province = string.Empty;
            TimeZoneID = string.Empty;
            StreetAddress = string.Empty;
            PostalCode = string.Empty;
            Name = string.Empty;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void NotifyPropertyChanged(string in_strPropertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(in_strPropertyName));
        }

        #endregion
    }
}
