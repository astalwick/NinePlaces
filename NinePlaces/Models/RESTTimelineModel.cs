using System;
using System.ComponentModel;
using System.Xml.Linq;

namespace NinePlaces.Models.REST
{
    public class RESTTimelineModel : RESTBaseModel
    {
        public override event ChildChangedEventHandler ChildAdded;
        public override event ChildChangedEventHandler ChildRemoved;
        public override event EventHandler SaveComplete;
        public override event EventHandler SaveError;
        public override event EventHandler LoadComplete;
        public override event EventHandler LoadError;

        // random number for unique ids.
        private static RESTSaveManager gSaveMgr = new RESTSaveManager();

        public RESTTimelineModel()
            : base(gSaveMgr)
        {
            UniqueID = 123;     // to remove! 

            m_strChildNodeName = "Vacation";
            m_strChildNodeContainerName = "Vacations";
            m_strThisNodeName = "Timeline";

            m_strModelURI = RESTSaveManager.CloudURL + "/NP.svc/arlen/timeline/" + UniqueID;
            // we should make sure that we're not holding on to anything.
            
            Empty();
        }

        protected override IUndoableModel ConstructChildModel(XElement in_xChildXMLNode, bool in_bAlreadyLoaded)
        {
            RESTVacationModel xNewIconModel = new RESTVacationModel(in_xChildXMLNode, this, m_oSaveMgr);
            xNewIconModel.Loaded = in_bAlreadyLoaded;
            return xNewIconModel as IUndoableModel;
        }

        #region IModel Members

        public override string PropertyFromParent(string in_strPropertyName)
        {
            // timeline model has no parent!
            throw new NotImplementedException();
        }

        public override bool PropertyToParent(string in_strPropertyName, string in_strPropertyValue)
        {
            // timeline model has no parent!
            throw new NotImplementedException();
        }

        public override IModel ParentModel
        {
            get
            {
                return null;
            }
        }

        #endregion

        #region INotifyPropertyChanged Members

        public override event PropertyChangedEventHandler PropertyChanged;

        #endregion

        protected override void InvokePropertyChanged(object sender, PropertyChangedEventArgs p)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(sender, p);
        }
        protected override void InvokeChildAdded(object sender, ChildChangedEventArgs p)
        {
            if (ChildAdded != null)
                ChildAdded.Invoke(sender, p);
        }
        protected override void InvokeChildRemoved(object sender, ChildChangedEventArgs p)
        {
            if (ChildRemoved != null)
                ChildRemoved.Invoke(sender, p);
        }
        protected override void InvokeSaveComplete(object sender, EventArgs p)
        {
            if (SaveComplete != null)
                SaveComplete.Invoke(sender, p);
        }
        protected override void InvokeSaveError(object sender, EventArgs p)
        {
            if (SaveError != null)
                SaveError.Invoke(sender, p);
        }
        protected override void InvokeLoadComplete(object sender, EventArgs p)
        {
            if (LoadComplete != null)
                LoadComplete.Invoke(sender, p);
        }
        protected override void InvokeLoadError(object sender, EventArgs p)
        {
            if (LoadError != null)
                LoadError.Invoke(sender, p);
        }
    }
}
