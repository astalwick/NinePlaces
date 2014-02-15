using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Xml.Linq;

namespace NinePlaces.Models.REST
{
    public class RESTIconModel : RESTBaseModel
    {
        public override event ChildChangedEventHandler ChildAdded;
        public override event ChildChangedEventHandler ChildRemoved;
        public override event EventHandler LoadComplete;
        public override event EventHandler LoadError;
        public override event EventHandler SaveComplete;
        public override event EventHandler SaveError;

        public string IconType
        {
            get;
            internal set;
        }

        public RESTIconModel(XElement in_xVacationRoot, RESTVacationModel in_oParentModel, RESTSaveManager in_oSaveMgr) : base( in_xVacationRoot, in_oParentModel, in_oSaveMgr )
        {
            m_strChildNodeName = "ERROR";
            m_strChildNodeContainerName = "ERROR";
            m_strThisNodeName = "Icon";

            m_strModelURI = RESTSaveManager.CloudURL + "/NP.svc/arlen/icon/" + UniqueID;

            IconType = m_oParentXMLRoot.Name.ToString();
            
            m_oRoot = new XElement(IconType, 
                new XAttribute("UniqueID", m_nUniqueID.ToString()), 
                new XElement("Properties"));
            m_oPropertiesRoot = m_oRoot.Element("Properties");
            m_oChildrenRoot = null;
        }


        protected override void RemoteLoadCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                InvokeLoadError(this, new EventArgs());
            }
            else
            {
                m_oRoot = XElement.Parse(e.Result);
                m_oPropertiesRoot = m_oRoot.Element("Properties");

                if (m_oRoot.Name == "EmptyResult")
                    throw new Exception( "Icon does not exist!" );

                DoneLoad();
            }
        }

        #region IModel Members

        #endregion


        public override IModel ChildFromID(int in_nID)
        {
            throw new NotImplementedException();
        }

        public override bool RemoveChild(IModel in_oNewVacation)
        {
            throw new NotImplementedException();
        }

        public override IModel NewChild(Dictionary<string, string> in_dictPropertiesToSet)
        {
            throw new NotImplementedException();
        }

        public override IModel NewChild(Dictionary<string, string> in_dictPropertiesToSet, int in_nUniqueID)
        {
            throw new NotImplementedException();
        }
        public override IModel NewChild()
        {
            throw new NotImplementedException();
        }

        public override List<IModel> GetChildModels()
        {
            throw new NotImplementedException();
        }

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
