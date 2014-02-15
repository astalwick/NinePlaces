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
    public class FlightIconViewModel : TravelIconViewModel
    {
        public FlightIconViewModel(ITimelineElementModel in_oXMLModel)
            : base(IconTypes.Flight, in_oXMLModel)
        {
        }

        private string m_strFlightNumber = "";
        public string FlightNumber
        {
            get
            {
                return m_strFlightNumber;
            }
            set
            {
                m_strFlightNumber = value;
                NotifyPropertyChanged("FlightNumber", m_strFlightNumber);
            }
        }
        private string m_strAirline = "";
        public string Airline
        {
            get
            {
                return m_strAirline;
            }
            set
            {
                m_strAirline = value;
                NotifyPropertyChanged("Airline", m_strAirline);
            }
        }

        protected override bool UpdatePropertyFromModel(string in_strProperty)
        {
            if (Model == null)
                return false;

            if (in_strProperty == "Airline" && Airline != Model.GetProperty("Airline"))
            {
                m_strAirline = Model.GetProperty("Airline");
                return true;
            }

            if (in_strProperty == "FlightNumber" && FlightNumber != Model.GetProperty("FlightNumber"))
            {
                m_strFlightNumber = Model.GetProperty("FlightNumber");
                return true;

            }
            return base.UpdatePropertyFromModel(in_strProperty);
        }
    }
}
