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
    public class CampingIconViewModel : LodgingViewModel
    {
        public CampingIconViewModel(ITimelineElementModel in_oXMLModel)
            : base(IconTypes.Camping, in_oXMLModel)
        {
        }

        private string m_strSiteNumber = "";
        public string SiteNumber
        {
            get
            {
                return m_strSiteNumber;
            }
            set
            {
                m_strSiteNumber = value;
                NotifyPropertyChanged("SiteNumber", m_strSiteNumber);
            }
        }

        protected override bool UpdatePropertyFromModel(string in_strProperty)
        {
            if (Model == null)
                return false;

            if (in_strProperty == "SiteNumber" && SiteNumber != Model.GetProperty("SiteNumber"))
            {
                m_strSiteNumber = Model.GetProperty("SiteNumber");
                return true;

            }
            return base.UpdatePropertyFromModel(in_strProperty);
        }
    }
}
