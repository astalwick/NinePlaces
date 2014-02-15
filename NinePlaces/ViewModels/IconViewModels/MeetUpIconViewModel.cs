using NinePlaces.Models;
using Common.Interfaces;
using System;

namespace NinePlaces.ViewModels
{
    public class MeetUpIconViewModel : ActivityIconViewModel, IWithPersonProperties
    {
        public MeetUpIconViewModel(ITimelineElementModel in_oXMLModel)
            : base(IconTypes.MeetUp, in_oXMLModel)
        {
        }

        private string m_strWho = "";
        public string Person
        {
            get
            {
                return m_strWho;
            }
            set
            {
                m_strWho = value;
                IconDescriptionDetails = m_strWho;
                NotifyPropertyChanged("Person", m_strWho);
            }
        }

        protected override bool UpdatePropertyFromModel(string in_strProperty)
        {
            if (Model == null)
                return false;

            if (in_strProperty == "Person" && m_strWho != Model.GetProperty("Person"))
            {
                m_strWho = Model.GetProperty("Person");
                IconDescriptionDetails = m_strWho;
                return true;
            }
            
            return base.UpdatePropertyFromModel(in_strProperty);
        }
    }
}
