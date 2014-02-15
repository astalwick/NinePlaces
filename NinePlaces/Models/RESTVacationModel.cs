using System;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;

namespace NinePlaces.Models.REST
{
    public class RESTVacationModel : RESTBaseModel, IIconContainerModel
    {
        public override event ChildChangedEventHandler ChildAdded;
        public override event ChildChangedEventHandler ChildRemoved;
        public override event EventHandler SaveComplete;
        public override event EventHandler SaveError;
        public override event EventHandler LoadComplete;
        public override event EventHandler LoadError;

        DateTime m_dtFirstIcon = new DateTime(9999, 1, 1);
        public DateTime FirstIconDate
        {
            get
            {
                if (m_dtFirstIcon.Year == 9999)
                    UpdateFirstLastIconDate();

                return m_dtFirstIcon;

            }
        }

        DateTime m_dtLastIcon = new DateTime(1, 1, 1);
        public DateTime LastIconDate
        {
            get
            {
                if (m_dtLastIcon.Year == 1)
                    UpdateFirstLastIconDate();

                return m_dtLastIcon;
            }
        }

        public RESTVacationModel(XElement in_xParentRoot, IModel in_oParentModel, RESTSaveManager in_oSaveMgr)
            : base(in_xParentRoot, in_oParentModel, in_oSaveMgr)
        {
            m_strChildNodeName = "Icon";
            m_strChildNodeContainerName = "Icons";
            m_strThisNodeName = "Vacation";
            
            m_strModelURI = RESTSaveManager.CloudURL + "/NP.svc/arlen/vacation/" + UniqueID;
            m_oRoot = new XElement(m_strThisNodeName,
                    new XElement(m_strChildNodeContainerName),
                    new XElement("Properties")
                    );

            m_oChildrenRoot = m_oRoot.Element(m_strChildNodeContainerName);
            m_oPropertiesRoot = m_oRoot.Element("Properties");

        }

        public override bool DoneLoad()
        {
            UpdateFirstLastIconDate();
            return base.DoneLoad(); 
        }

        public override bool DoneSave()
        {
            UpdateFirstLastIconDate();
            return base.DoneSave();
        }

        private void UpdateFirstLastIconDate()
        {
            var icons = from icon in m_oChildrenRoot.Elements()
                        where icon.Element("DockTime") != null 
                        select new
                        {
                            iconticks = icon.Element("DockTime").Value
                        };

            if (icons.Count() == 0)
                return;

            long lFirstTicks = long.MaxValue;
            long lLastTicks = long.MinValue;
            foreach (var ticks in icons)
            {
                long lTicks = Convert.ToInt64(ticks.iconticks);
                if (lTicks < lFirstTicks)
                    lFirstTicks = lTicks;
                if (lTicks > lLastTicks)
                    lLastTicks = lTicks;
            }

            m_dtLastIcon = new DateTime( lLastTicks );
            m_dtFirstIcon = new DateTime( lFirstTicks );
        }

        protected override IUndoableModel ConstructChildModel(XElement in_xChildXMLNode, bool in_bAlreadyLoaded)
        {
            RESTIconModel xNewIconModel = new RESTIconModel(in_xChildXMLNode, this, m_oSaveMgr);
            xNewIconModel.Loaded = in_bAlreadyLoaded;
            return xNewIconModel as IUndoableModel;
        }

        #region INotifyPropertyChanged Members

        public override event PropertyChangedEventHandler PropertyChanged;

        #endregion

        protected override void InvokePropertyChanged(object sender, PropertyChangedEventArgs p)
        {
            if( PropertyChanged != null )
                PropertyChanged.Invoke( sender,  p );
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
