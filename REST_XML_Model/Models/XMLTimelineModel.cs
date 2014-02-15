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
using System.Xml.Linq;
using System.Linq;
using System.IO;
using System.IO.IsolatedStorage;
using System.Collections.Generic;
using System.ComponentModel;

namespace NinePlaces.Models.ISO
{
    public class XMLTimelineModel : IUndoableModel
    {
        public event ChildChangedEventHandler ChildAdded;
        public event ChildChangedEventHandler ChildRemoved;

        private object FileReadWriteLock = new object();
        // random number for unique ids.
        private Random r = new Random();
        private static XMLSaveManager m_oSaveMgr = new XMLSaveManager();

        private XElement m_oGlobalPropsRoot = null;
        private XElement m_oTimelineRoot = null;
        private XElement m_oVacationsRoot = null;
        private XElement m_oPropertiesRoot = null;
         
        public event EventHandler SaveComplete;
        public event EventHandler SaveError;
        public event EventHandler LoadComplete;
        public event EventHandler LoadError;

        public Dictionary<int, XMLVacationModel> m_oVacationXMLModels = new Dictionary<int, XMLVacationModel>();

        public bool Loaded
        {
            get;
            internal set;
        }

        public int UniqueID
        {
            get;
            internal set;
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

        public XMLTimelineModel()
        {
            // we should make sure that we're not holding on to anything.
            UniqueID = 111;     // to remove! 

            EmptyTimeline();
        }

        #region IModelPersistable Members

        public bool RemoveYourself()
        {
            lock (FileReadWriteLock)
            {
                try
                {
                    if (AllowPersist)
                    {
                        using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            if (isf.FileExists("timeline_" + UniqueID.ToString() + ".xml"))
                                isf.DeleteFile("timeline_" + UniqueID.ToString() + ".xml");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("RESTTimelineModel RemoveYourself error: " + ex.Message);
                    System.Diagnostics.Debug.Assert(false);
                    return false;
                }

                return true;
            }
        }

        public bool ISOSave()
        {
            lock (FileReadWriteLock)
            {
                try
                {
                    using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream("timeline_" + UniqueID.ToString() + ".xml", FileMode.Create, isf))
                        {
                            using (StreamWriter sw = new StreamWriter(isfs))
                            {
                                m_oTimelineRoot.Save(sw);
                                App.BytesWritten += sw.BaseStream.Length;

                                System.Diagnostics.Debug.WriteLine("SAVED (" + sw.BaseStream.Length.ToString() + " bytes - " + App.BytesWritten.ToString() + "): TIMELINE ROOT");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("error in RESTTimelineModel IsoSave! --- " + ex.Message);
                    System.Diagnostics.Debug.Assert(false);
                    return false;
                }

                return true;
            }
        }

        public bool ISOLoad()
        {
            lock (FileReadWriteLock)
            {
                try
                {
                    string data = String.Empty;
                    using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (isf.FileExists("timeline_" + UniqueID.ToString() + ".xml"))
                        {
                            using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream("timeline_" + UniqueID.ToString() + ".xml", FileMode.Open, isf))
                            {
                                using (StreamReader sr = new StreamReader(isfs))
                                {
                                    m_oTimelineRoot = XElement.Load(sr);
                                    App.BytesRead += sr.BaseStream.Length;

                                    System.Diagnostics.Debug.WriteLine("LOADED (" + sr.BaseStream.Length.ToString() + " bytes - " + App.BytesRead.ToString() + "): TIMELINE ROOT");
                                }
                            }
                        }
                        else
                        {
                            EmptyTimeline();
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("error in RESTTimelineModel IsoLoad (depersisting)! --- " + ex.Message);
                    System.Diagnostics.Debug.Assert(false);
                    return false;
                }

                try
                {
                    m_oGlobalPropsRoot = m_oTimelineRoot.Element("GlobalProperties");
                    m_oVacationsRoot = m_oTimelineRoot.Element("Vacations");
                    m_oPropertiesRoot = m_oTimelineRoot.Element("Properties");


                    //
                    // create all the IconXMLModels
                    //

                    var vacations = from vacation in m_oVacationsRoot.Elements()
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
                        XMLVacationModel icm = new XMLVacationModel(w.item, this, m_oSaveMgr);
                        m_oVacationXMLModels.Add(Convert.ToInt32(w.uniqueid), icm);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("error in XMLModel IsoLoad (parsing children)! --- " + ex.Message);
                    System.Diagnostics.Debug.Assert(false);
                    return false;
                }
                return true;
            }
        }

        public bool DoneLoad()
        {
            //
            // ok, for every PROPERTY in this model, we need to fire a notification.
            //

            Loaded = true;

            if (PropertyChanged != null)
            {
                var vProperties = from prop in m_oPropertiesRoot.Elements()
                                  select new
                                  {
                                      propName = prop.Name.LocalName
                                  };

                foreach (var prop in vProperties)
                {
                    string strPropertyName = prop.propName;

                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(strPropertyName));
                }
            }

            
            if (LoadComplete != null)
                LoadComplete.Invoke(this, new EventArgs());

            return true;
        }

        public bool DoneSave()
        {
            if (SaveComplete != null)
                SaveComplete.Invoke(this, new EventArgs());
            return true;
        }

        #endregion

        private bool EmptyTimeline()
        {
            m_oTimelineRoot = new XElement("Timeline",
                new XElement("GlobalProperties"),
                new XElement("Vacations"),
                new XElement("Properties")

            );

            foreach (XMLVacationModel vm in m_oVacationXMLModels.Values)
            {
                vm.RemoveYourself();
                m_oSaveMgr.RemoveSave(vm);
                m_oSaveMgr.RemoveLoad(vm);
            }

            m_oVacationXMLModels.Clear();

            m_oGlobalPropsRoot = m_oTimelineRoot.Element("GlobalProperties");
            m_oVacationsRoot = m_oTimelineRoot.Element("Vacations");
            m_oPropertiesRoot = m_oTimelineRoot.Element("Vacations");
            return true;
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

        public bool RemoveChild(IModel in_oChild)
        {
            var icons = from icon in m_oVacationsRoot.Elements()
                        where icon.Attributes("UniqueID") != null && icon.Attribute("UniqueID").Value == in_oChild.UniqueID.ToString()
                        select new
                        {
                            iconroot = icon
                        };

            if (icons.Count() != 1)
            {
                System.Diagnostics.Debug.WriteLine("Removing an child with multiple instances: " + icons.Count().ToString());
                System.Diagnostics.Debug.Assert(false);
                return false;
            }

            // ask the child to delete itself!  (this will permanently remove it!)
            m_oVacationXMLModels[in_oChild.UniqueID].RemoveYourself();

            // remove it from our list.
            m_oVacationXMLModels.Remove(in_oChild.UniqueID);

            // make sure it's not being saved or loaded.
            m_oSaveMgr.RemoveSave(in_oChild);
            m_oSaveMgr.RemoveLoad(in_oChild);

            // finally, remove it from the vacation itself.
            icons.ElementAt(0).iconroot.Remove();

            App.UndoMgr.ChildChanged(this, UniqueID, in_oChild.UniqueID, false, in_oChild.AllProperties());
            if (ChildRemoved != null)
                ChildRemoved.Invoke(this, new ChildChangedEventArgs(UniqueID, in_oChild.UniqueID, false));
            MustSave = true;

            return true;
        }

        public Dictionary<string, string> AllProperties()
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
                d.Add( prop.propName, prop.propValue );
            }

            return d;
        }

        public IModel NewChild(Dictionary<string, string> in_dictPropertiesToSet, int in_nUniqueID)
        {

            // create the child xml node.
            XElement x = new XElement("Vacation", new XAttribute("UniqueID", in_nUniqueID.ToString()));
            // add it to our vacation
            m_oVacationsRoot.Add(x);

            // create a new model for this child
            XMLVacationModel xNewVacationModel = new XMLVacationModel(x, this, m_oSaveMgr);

            // since we're creating it, there's nothing to load - it's all done already
            xNewVacationModel.Loaded = true;

            // add it to our ID <-> IconModel dictionary.
            m_oVacationXMLModels.Add(in_nUniqueID, xNewVacationModel);


            if (in_dictPropertiesToSet != null)
            {
                foreach (string strProperty in in_dictPropertiesToSet.Keys)
                {
                    xNewVacationModel.UpdateProperty(strProperty, in_dictPropertiesToSet[strProperty]);

                    //anything that is set on creation is set into the parent as well.
                    xNewVacationModel.PropertyToParent(strProperty, in_dictPropertiesToSet[strProperty]);
                }
            }

            App.UndoMgr.ChildChanged(this, UniqueID, in_nUniqueID, true, in_dictPropertiesToSet);
            if (ChildAdded != null)
                ChildAdded.Invoke(this, new ChildChangedEventArgs(UniqueID, in_nUniqueID, true));

            // make sure we persist this change.
            MustSave = true;

            return xNewVacationModel as IModel;
        }




        public IModel ChildFromID(int in_nID)
        {
            return m_oVacationXMLModels[in_nID] as IModel;
        }

        public IModel NewChild(Dictionary<string, string> in_dictPropertiesToSet)
        {
            // generate an id.
            // loop until it's unique.
            int nID = 0;
            do
            {
                nID = r.Next();
            }
            while (m_oVacationXMLModels.ContainsKey(nID));

            return NewChild(in_dictPropertiesToSet, nID);
        }

        public IModel NewChild()
        {
            return NewChild(null);
 
        }

        public List<IModel> GetChildModels()
        {
            List<IModel> oToRet = new List<IModel>();
            foreach (XMLVacationModel v in m_oVacationXMLModels.Values)
            {
                oToRet.Add(v as IModel);
            }
            return oToRet;
        }

        #region IModel Members

        public bool HasProperty(string in_strPropertyName)
        {
            if (!Loaded)
            {
                return PropertyFromParent(in_strPropertyName) != "";
            }

            return m_oPropertiesRoot.Element(in_strPropertyName) != null;
        }

        public string PropertyFromParent(string in_strPropertyName)
        {
            // timeline model has no parent!
            throw new NotImplementedException();
        }


        public bool PropertyToParent(string in_strPropertyName, string in_strPropertyValue)
        {
            // timeline model has no parent!
            throw new NotImplementedException();
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
            if (m_oPropertiesRoot.Element(in_strPropertyName) == null)
            {
                // the property isn't there! create it and insertit.
                m_oPropertiesRoot.Add(new XElement(in_strPropertyName, in_strPropertyValue));
            }
            else
            {
                strPreviousValue = m_oPropertiesRoot.Element(in_strPropertyName).Value;
                m_oPropertiesRoot.Element(in_strPropertyName).Value = in_strPropertyValue;
            }

            if (!in_bNoUndo)
                App.UndoMgr.PropertyChanged(this, UniqueID, in_strPropertyName, strPreviousValue, in_strPropertyValue);
            if (!in_bNoNotify)
            {
                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(in_strPropertyName));
            }

            MustSave = true;

            return true;
        }

        public IModel ParentModel
        {
            get
            {
                return null;
            }
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region IUndoableModel Members

        public bool UndoRedoSetProperty(string in_strPropertyName, string in_strNewValue)
        {
            if (!Loaded)
            {
                return false;
            }

            System.Diagnostics.Debug.WriteLine("UndoSetProperty: " + in_strPropertyName);
            if (m_oPropertiesRoot.Element(in_strPropertyName) == null)
            {
                if (in_strNewValue != null)    // if we're setting it to null.. and we haven't got an element that matches.. then we're good, no?
                    m_oPropertiesRoot.Add(new XElement(in_strPropertyName, in_strNewValue));

            }
            else
            {
                if (in_strNewValue != null)
                    m_oPropertiesRoot.Element(in_strPropertyName).Value = in_strNewValue;
                else
                    m_oPropertiesRoot.Element(in_strPropertyName).Remove();
            }

            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(in_strPropertyName));

            MustSave = true;
            return true;
        }

        #endregion


        #region IPersistableModel Members


        public void EmergencySave()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
