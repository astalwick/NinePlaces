using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Windows.Browser;
using System.Xml.Linq;
using NinePlaces.Models.REST;

namespace NinePlaces.Models
{
    public class RESTBaseModel : IUndoableModel
    {
        protected string m_strChildNodeName = "base_ERROR";
        protected string m_strChildNodeContainerName = "base_ERROR";
        protected string m_strThisNodeName = "base_ERROR";
        protected string m_strModelURI = "ERROR";

        protected object FileReadWriteLock = new object();

        // random number for unique ids.
        protected Random r = new Random();

        // save manager
        protected int m_nUniqueID = 0;
        protected XElement m_oRoot = null;
        protected XElement m_oParentXMLRoot = null;
        protected XElement m_oChildrenRoot = null;
        protected XElement m_oPropertiesRoot = null;

        protected RESTSaveManager m_oSaveMgr = null;
        protected IModel m_oParentModel = null;
        protected Dictionary<int, IUndoableModel> m_oChildModels = new Dictionary<int, IUndoableModel>();


        public virtual int UniqueID
        {
            get
            {
                return m_nUniqueID;
            }
            internal set
            {
                m_nUniqueID = value;
            }

        }
        public virtual IModel ParentModel
        {
            get
            {
                return m_oParentModel as IModel;
            }
        }

        // some changed state info.
        private bool m_bMustSave = false;
        protected bool MustSave
        {
            get
            {
                return m_bMustSave;
            }
            set
            {
                m_bMustSave = true;
                m_oSaveMgr.EnqueueToSave(this as IPersistableModel, true);
            }
        }

        public RESTBaseModel(RESTSaveManager in_oSaveMgr)
        {
            m_oSaveMgr = in_oSaveMgr;
        }

        public RESTBaseModel(XElement in_xParentRoot, IModel in_oParentModel, RESTSaveManager in_oSaveMgr)
        {
            m_oSaveMgr = in_oSaveMgr;
            m_oParentXMLRoot = in_xParentRoot;
            UniqueID = Convert.ToInt32(m_oParentXMLRoot.Attribute("UniqueID").Value);
            m_oParentModel = in_oParentModel;
        }

        #region IUndoableModel Members

        public bool UndoRedoSetProperty(string in_strPropertyName, string in_strNewValue)
        {
            return UpdateProperty(in_strPropertyName, in_strNewValue, false, true);
        }

        #endregion


        #region IHierarchicalModel Members



        public virtual bool HasProperty(string in_strPropertyName)
        {
            if (!Loaded)
            {
                return PropertyFromParent(in_strPropertyName) != "";
            }

            return m_oPropertiesRoot.Element(in_strPropertyName) != null;
        }

        public virtual string PropertyFromParent(string in_strPropertyName)
        {
            if (m_oParentXMLRoot.Element(in_strPropertyName) != null)
                return m_oParentXMLRoot.Element(in_strPropertyName).Value;

            return "";
        }

        public virtual bool PropertyToParent(string in_strPropertyName, string in_strPropertyValue)
        {
            if (m_oParentXMLRoot.Element(in_strPropertyName) == null)
            {
                // the property isn't there! create it and insertit.
                m_oParentXMLRoot.Add(new XElement(in_strPropertyName, in_strPropertyValue));
            }
            else
            {
                m_oParentXMLRoot.Element(in_strPropertyName).Value = in_strPropertyValue;
            }
            m_oParentModel.Save();

            return true;
        }

        public string PropertyValue(string in_strPropertyName)
        {
            if (!Loaded)
            {
                return PropertyFromParent(in_strPropertyName);
            }

            return m_oPropertiesRoot.Element(in_strPropertyName) != null ? m_oPropertiesRoot.Element(in_strPropertyName).Value : "";
        }

        public bool UpdateProperty(string in_strPropertyName, string in_strPropertyValue)
        {
            return UpdateProperty(in_strPropertyName, in_strPropertyValue, false, false);
        }

        public bool UpdateProperty(string in_strPropertyName, string in_strPropertyValue, bool in_bNoNotify, bool in_bNoUndo)
        {
            if (!Loaded)
            {
                return false;
            }

            string strPreviousValue = null;
            System.Diagnostics.Debug.WriteLine("UpdateProperty: " + in_strPropertyName);
            if (m_oPropertiesRoot.Element(in_strPropertyName) == null)
            {
                if (in_strPropertyValue != null)    // if we're setting it to null.. and we haven't got an element that matches.. then we're good, no?
                    m_oPropertiesRoot.Add(new XElement(in_strPropertyName, in_strPropertyValue));

            }
            else
            {
                strPreviousValue = m_oPropertiesRoot.Element(in_strPropertyName).Value;
                if (in_strPropertyValue != null)
                    m_oPropertiesRoot.Element(in_strPropertyName).Value = in_strPropertyValue;
                else
                    m_oPropertiesRoot.Element(in_strPropertyName).Remove();
            }

            if (!in_bNoUndo)
                App.UndoMgr.PropertyChanged(this, UniqueID, in_strPropertyName, strPreviousValue, in_strPropertyValue);
            if (!in_bNoNotify)
            {
                InvokePropertyChanged(this, new PropertyChangedEventArgs(in_strPropertyName));
            }

            MustSave = true;

            return true;
        }

        public virtual IModel NewChild(Dictionary<string, string> in_dictPropertiesToSet)
        {
            // generate an id.
            // loop until it's unique.
            int nID = 0;
            do
            {
                nID = r.Next();
            }
            while (m_oChildModels.ContainsKey(nID));

            return NewChild(in_dictPropertiesToSet, nID);
        }

        public virtual IModel NewChild()
        {
            return NewChild(null);
        }

        public virtual IModel NewChild(Dictionary<string, string> in_dictPropertiesToSet, int in_nUniqueID)
        {

            // create the child xml node.
            XElement x = new XElement(m_strChildNodeName, new XAttribute("UniqueID", in_nUniqueID.ToString()));
            // add it to our vacation
            m_oChildrenRoot.Add(x);

            // create a new model for this child
            IUndoableModel xChildModel = ConstructChildModel(x, true);

            // add it to our ID <-> IconModel dictionary.
            m_oChildModels.Add(in_nUniqueID, xChildModel);

            if (in_dictPropertiesToSet != null)
            {
                foreach (string strProperty in in_dictPropertiesToSet.Keys)
                {
                    xChildModel.UpdateProperty(strProperty, in_dictPropertiesToSet[strProperty]);

                    //anything that is set on creation is set into the parent as well.
                    xChildModel.PropertyToParent(strProperty, in_dictPropertiesToSet[strProperty]);
                }
            }

            App.UndoMgr.ChildChanged(this, UniqueID, in_nUniqueID, true, in_dictPropertiesToSet);

            InvokeChildAdded(this, new ChildChangedEventArgs(UniqueID, in_nUniqueID, true));

            // make sure we persist this change.
            MustSave = true;

            return xChildModel as IModel;
        }

        public virtual bool RemoveChild(IModel in_oChildModel)
        {
            var children = from child in m_oChildrenRoot.Elements()
                           where child.Attributes("UniqueID") != null && child.Attribute("UniqueID").Value == in_oChildModel.UniqueID.ToString()
                           select new
                           {
                               childroot = child
                           };

            if (children.Count() != 1)
            {
                System.Diagnostics.Debug.WriteLine("Removing a child with multiple instances: " + children.Count().ToString());
                System.Diagnostics.Debug.Assert(false);
                return false;
            }

            // ask the child to delete itself!  (this will permanently remove it!)
            m_oChildModels[in_oChildModel.UniqueID].RemoveYourself();

            // remove it from our list.
            m_oChildModels.Remove(in_oChildModel.UniqueID);

            // make sure it's not being saved or loaded.
            m_oSaveMgr.RemoveSave(in_oChildModel);
            m_oSaveMgr.RemoveLoad(in_oChildModel);

            // finally, remove it from the vacation itself.
            children.ElementAt(0).childroot.Remove();

            App.UndoMgr.ChildChanged(this, UniqueID, in_oChildModel.UniqueID, false, in_oChildModel.AllProperties());
            InvokeChildRemoved(this, new ChildChangedEventArgs(UniqueID, in_oChildModel.UniqueID, false));
            MustSave = true;

            return true;
        }

        protected virtual bool Empty()
        {
            m_oRoot = new XElement(m_strThisNodeName,
                    new XElement(m_strChildNodeContainerName),
                    new XElement("Properties")
                    );

            foreach (IModel im in m_oChildModels.Values)
            {
                im.RemoveYourself();
                m_oSaveMgr.RemoveSave(im);
                m_oSaveMgr.RemoveLoad(im);
            }

            m_oChildModels.Clear();
            m_oPropertiesRoot = m_oRoot.Element("Properties");
            m_oChildrenRoot = m_oRoot.Element(m_strChildNodeContainerName);
            return true;
        }

        public virtual IModel ChildFromID(int in_nID)
        {
            return m_oChildModels[in_nID] as IModel;
        }

        public virtual List<IModel> GetChildModels()
        {
            List<IModel> oToRet = new List<IModel>();
            foreach (IModel v in m_oChildModels.Values)
            {
                oToRet.Add(v);
            }
            return oToRet;
        }

        public virtual event ChildChangedEventHandler ChildAdded;

        public virtual event ChildChangedEventHandler ChildRemoved;

        #endregion

        #region IPropertyModel Members

        public virtual Dictionary<string, string> AllProperties()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();

            var vProperties = from prop in m_oPropertiesRoot.Elements()
                              select new
                              {
                                  propName = prop.Name.LocalName,
                                  propValue = prop.Value
                              };

            foreach (var prop in vProperties)
            {
                d.Add(prop.propName, prop.propValue);
            }

            return d;
        }

        #endregion

        #region INotifyPropertyChanged Members

        public virtual event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region IPersistableModel Members

        public bool ISOSave()
        {

            //Get the list of available timelines
            Uri uri = new Uri(m_strModelURI);
            System.Diagnostics.Debug.WriteLine("Saved: " + m_strModelURI);
            WebClient client = new WebClient();
            client.UploadStringCompleted += new UploadStringCompletedEventHandler(RemoteSaveCompleted);
            client.Headers["Content-Type"] = "application/xml";
            client.Headers["X-HTTP-Method-Override"] = "PUT";
            client.UploadStringAsync(uri, "POST", m_oRoot.ToString());
            return true;
        }

        public bool ISOLoad()
        {
            Uri uri = new Uri(m_strModelURI);
            WebClient client = new WebClient();
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(RemoteLoadCompleted);
            client.DownloadStringAsync(uri);

            return true;
        }

        protected virtual void RemoteLoadCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                InvokeLoadError(this, new EventArgs());
                Empty();
                DoneLoad();
            }
            else
            {
                m_oRoot = XElement.Parse(e.Result);

                if (m_oRoot.Name == "EmptyResult")
                    Empty();

                try
                {
                    m_oChildrenRoot = m_oRoot.Element(m_strChildNodeContainerName);
                    m_oPropertiesRoot = m_oRoot.Element("Properties");


                    //
                    // create all the IconXMLModels
                    //

                    var vacations = from vacation in m_oChildrenRoot.Elements()
                                    where vacation.Attributes("UniqueID") != null
                                    select new
                                    {
                                        uniqueid = vacation.Attribute("UniqueID").Value,
                                        item = vacation
                                    };

                    List<int> lToRet = new List<int>();
                    int n = 0;
                    foreach (var w in vacations)
                    {
                        n++;
                        IUndoableModel icm = ConstructChildModel(w.item, false);
                        m_oChildModels.Add(Convert.ToInt32(w.uniqueid), icm);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("error in XMLModel IsoLoad (parsing children)! --- " + ex.Message);
                    System.Diagnostics.Debug.Assert(false);
                    InvokeLoadError(this, new EventArgs());
                }

                DoneLoad();
            }
        }

        protected virtual void RemoteSaveCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            DoneSave();
        }

        public virtual bool DoneLoad()
        {
            //
            // ok, for every PROPERTY in this model, we need to fire a notification.
            //
            Loaded = true;
            var vProperties = from prop in m_oPropertiesRoot.Elements()
                              select new
                              {
                                  propName = prop.Name.LocalName
                              };

            foreach (var prop in vProperties)
            {
                InvokePropertyChanged(this, new PropertyChangedEventArgs(prop.propName));
            }

            InvokeLoadComplete(this, new EventArgs());

            return true;
        }

        public virtual bool DoneSave()
        {
            InvokeSaveComplete(this, new EventArgs());

            return true;
        }

        public bool RemoveYourself()
        {
            Uri uri = new Uri(m_strModelURI);

            WebClient client = new WebClient();
            client.Headers["Content-Type"] = "application/xml";
            client.Headers["X-HTTP-Method-Override"] = "DELETE";
            client.UploadStringAsync(uri, "POST", "<Delete />");

            return true;
        }

        public virtual event EventHandler SaveComplete;

        public virtual event EventHandler SaveError;

        public virtual event EventHandler LoadComplete;

        public virtual event EventHandler LoadError;

        public bool Loaded
        {
            get;
            internal set;
        }

        public bool Load()
        {
            m_oSaveMgr.EnqueueToLoad(this as IPersistableModel, true);
            return true;
        }

        public bool Save()
        {
            m_oSaveMgr.EnqueueToSave(this as IPersistableModel, true);
            return true;
        }

        public bool DelayPersist
        {
            get
            {
                return m_oSaveMgr.DelayPersist;
            }
            set
            {
                m_oSaveMgr.DelayPersist = value;
            }
        }

        public bool AllowPersist
        {
            get
            {
                return m_oSaveMgr.AllowPersist;
            }
            set
            {
                m_oSaveMgr.AllowPersist = value;
            }
        }


        public void EmergencySave()
        {
            if (App.Current.Host.Source.AbsoluteUri.Contains("nineplaces.cloudapp.net"))
            {
                ScriptObject xhr = HtmlPage.Window.CreateInstance("XMLHttpRequest");
                xhr.Invoke("open", "POST", m_strModelURI, false);
                xhr.Invoke("setRequestHeader", "X-HTTP-Method-Override", "PUT");
                xhr.Invoke("setRequestHeader", "Content-Type", "application/xml");
                xhr.Invoke("send", m_oRoot.ToString());

                string strResponse = (string)xhr.GetProperty("responseText");
            }
        }

        #endregion

        public override string ToString()
        {
            return m_oRoot.ToString();
        }

        protected virtual IUndoableModel ConstructChildModel(XElement in_xChildXMLNode, bool in_bAlreadyLoaded)
        {
            throw new NotImplementedException();
        }

        protected virtual void InvokePropertyChanged(object sender, PropertyChangedEventArgs p)
        {
            throw new NotImplementedException();
        }
        protected virtual void InvokeChildAdded(object sender, ChildChangedEventArgs p)
        {
            throw new NotImplementedException();
        }
        protected virtual void InvokeChildRemoved(object sender, ChildChangedEventArgs p)
        {
            throw new NotImplementedException();
        }
        protected virtual void InvokeSaveComplete(object sender, EventArgs p)
        {
            throw new NotImplementedException();
        }
        protected virtual void InvokeSaveError(object sender, EventArgs p)
        {
            throw new NotImplementedException();
        }
        protected virtual void InvokeLoadComplete(object sender, EventArgs p)
        {
            throw new NotImplementedException();
        }
        protected virtual void InvokeLoadError(object sender, EventArgs p)
        {
            throw new NotImplementedException();
        }
    }
}
