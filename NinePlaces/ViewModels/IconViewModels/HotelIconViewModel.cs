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

namespace NinePlaces.ViewModels
{
    public class HotelIconViewModel : LodgingViewModel
    {
        public HotelIconViewModel(ITimelineElementModel in_oXMLModel)
            : base(IconTypes.Hotel, in_oXMLModel)
        {
        }

        private string m_strRoomNumber = "";
        public string RoomNumber
        {
            get
            {
                return m_strRoomNumber;
            }
            set
            {
                m_strRoomNumber = value;
                NotifyPropertyChanged("RoomNumber", m_strRoomNumber);
            }
        }

        protected override bool UpdatePropertyFromModel(string in_strProperty)
        {
            if (Model == null)
                return false;

            if (in_strProperty == "RoomNumber" && RoomNumber != Model.GetProperty("RoomNumber"))
            {
                m_strRoomNumber = Model.GetProperty("RoomNumber");
                return true;

            }
            return base.UpdatePropertyFromModel(in_strProperty);
        }
    }
}
