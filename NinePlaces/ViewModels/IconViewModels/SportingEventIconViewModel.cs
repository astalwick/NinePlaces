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
    public class SportingEventIconViewModel : ActivityIconViewModel
    {
        public SportingEventIconViewModel(ITimelineElementModel in_oXMLModel)
            : base(IconTypes.SportingEvent, in_oXMLModel)
        {
        }

        private string m_strWho = "";
        public string SportingEventPlaying
        {
            get
            {
                return m_strWho;
            }
            set
            {
                m_strWho = value;
                IconDescriptionDetails = m_strWho;
                NotifyPropertyChanged("SportingEventPlaying", m_strWho);
            }
        }

        protected override bool UpdatePropertyFromModel(string in_strProperty)
        {
            if (Model == null)
                return false;

            if (in_strProperty == "SportingEventPlaying" && m_strWho != Model.GetProperty("SportingEventPlaying"))
            {
                m_strWho = Model.GetProperty("SportingEventPlaying");
                IconDescriptionDetails = m_strWho;
                return true;
            }

            return base.UpdatePropertyFromModel(in_strProperty);
        }
    }
}
