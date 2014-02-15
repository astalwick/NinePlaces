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
using System.Windows.Threading;

namespace NinePlaces.Models.ISO
{
    public class XMLVacationModel : IUndoableModel, IIconContainerModel
    {
        public event ChildChangedEventHandler ChildAdded;
        public event ChildChangedEventHandler ChildRemoved;

        private object FileReadWriteLock = new object();

        // random number for unique ids.
        private Random r = new Random();

        // save manager

        private int m_nUniqueID = 0;
        private XElement m_oVacationXMLRoot = null;
        private XElement m_oTimelineXMLNode = null;

        private XMLSaveManager m_oSaveMgr = null;
        private XMLTimelineModel m_oParentModel = null;

        public event EventHandler SaveComplete;
        public event EventHandler SaveError;
        public event EventHandler LoadComplete;
        public event EventHandler LoadError;


        public bool Loaded
        {
            get;
            internal set;
        }

        public Dictionary<int, XMLIconModel> m_oIconXMLModels = new Dictionary<int, XMLIconModel>();
        
        private XElement m_oIconsRoot = null;
        private XElement m_oPropertiesRoot = null;

        // some changed state info.
        private bool m_bMustSave = false;
        private bool MustSave
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

        public int UniqueID
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


        public XMLVacationModel(XElement in_xTimelineRoot, XMLTimelineModel in_oParentModel, XMLSaveManager in_oSaveMgr)
        {
            m_oSaveMgr = in_oSaveMgr;
            m_oTimelineXMLNode = in_xTimelineRoot;
            UniqueID = Convert.ToInt32(m_oTimelineXMLNode.Attribute("UniqueID").Value);
            
            m_oParentModel = in_oParentModel;

            m_oVacationXMLRoot = new XElement("Vacation",
                    new XElement("GlobalProperties"),
                    new XElement("Icons"),
                    new XElement("Properties")
                    );

            m_oIconsRoot = m_oVacationXMLRoot.Element("Icons");
            m_oPropertiesRoot = m_oVacationXMLRoot.Element("Properties");

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

            UpdateFirstLastIconDate();



            if (LoadComplete != null)
                LoadComplete.Invoke(this, new EventArgs());
            return true;
        }

        public bool DoneSave()
        {
            UpdateFirstLastIconDate();

            if (SaveComplete != null)
                SaveComplete.Invoke(this, new EventArgs());
            return true;
        }

        private void UpdateFirstLastIconDate()
        {
            var icons = from icon in m_oIconsRoot.Elements()
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
                d.Add(prop.propName, prop.propValue);
            }

            return d;
        }

        public bool RemoveChild(IModel in_oIconModel)
        {
            var icons = from icon in m_oIconsRoot.Elements()
                        where icon.Attributes("UniqueID") != null && icon.Attribute("UniqueID").Value == in_oIconModel.UniqueID.ToString()
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
            m_oIconXMLModels[in_oIconModel.UniqueID].RemoveYourself();

            // remove it from our list.
            m_oIconXMLModels.Remove(in_oIconModel.UniqueID);

            // make sure it's not being saved or loaded.
            m_oSaveMgr.RemoveSave(in_oIconModel);
            m_oSaveMgr.RemoveLoad(in_oIconModel);

            // finally, remove it from the vacation itself.
            icons.ElementAt(0).iconroot.Remove();

            App.UndoMgr.ChildChanged(this, UniqueID, in_oIconModel.UniqueID, false, in_oIconModel.AllProperties());
            if (ChildRemoved != null)
                ChildRemoved.Invoke(this, new ChildChangedEventArgs(UniqueID, in_oIconModel.UniqueID, false));
            MustSave = true;

            return true;
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
            while (m_oIconXMLModels.ContainsKey(nID));

            return NewChild(in_dictPropertiesToSet, nID);
        }
        public IModel NewChild(Dictionary <string,string> in_dictPropertiesToSet, int in_nUniqueID)
        {

            // create the child xml node.
            XElement x = new XElement("Icon", new XAttribute("UniqueID", in_nUniqueID.ToString()));
            // add it to our vacation
            m_oIconsRoot.Add(x);

            // create a new model for this child
            XMLIconModel xNewIconModel = new XMLIconModel(x, this, m_oSaveMgr);

            // since we're creating it, there's nothing to load - it's all done already
            xNewIconModel.Loaded = true;

            // add it to our ID <-> IconModel dictionary.
            m_oIconXMLModels.Add(in_nUniqueID, xNewIconModel);

            if (in_dictPropertiesToSet != null)
            {
                foreach (string strProperty in in_dictPropertiesToSet.Keys)
                {
                    xNewIconModel.UpdateProperty(strProperty, in_dictPropertiesToSet[strProperty]);

                    //anything that is set on creation is set into the parent as well.
                    xNewIconModel.PropertyToParent(strProperty, in_dictPropertiesToSet[strProperty]);
                }
            }

            App.UndoMgr.ChildChanged(this, UniqueID, in_nUniqueID, true, in_dictPropertiesToSet);

            if (ChildAdded != null)
                ChildAdded.Invoke( this, new ChildChangedEventArgs( UniqueID, in_nUniqueID, true ));

            // make sure we persist this change.
            MustSave = true;

            return xNewIconModel as IModel;
        }

        public IModel ChildFromID(int in_nID)
        {
            return m_oIconXMLModels[in_nID] as IModel;
        }

        public IModel NewChild()
        {
            return NewChild( null );
        }

        public List<IModel> GetChildModels()
        {
            List<IModel> oToRet = new List<IModel>();
            foreach (XMLIconModel v in m_oIconXMLModels.Values)
            {
                oToRet.Add(v as IModel);
            }
            return oToRet;
        }

        private bool EmptyVacation()
        {
            m_oVacationXMLRoot = new XElement("Vacation",
                    new XElement("GlobalProperties"),
                    new XElement("Icons"),
                    new XElement("Properties")
                    );

            foreach (XMLIconModel im in m_oIconXMLModels.Values)
            {
                im.RemoveYourself();
                m_oSaveMgr.RemoveSave(im);
                m_oSaveMgr.RemoveLoad(im);
            }

            m_oIconXMLModels.Clear();
            m_oPropertiesRoot = m_oVacationXMLRoot.Element("Properties");
            m_oIconsRoot = m_oVacationXMLRoot.Element("Icons");
            return true;
        }

        public override string ToString()
        {
            return m_oVacationXMLRoot.ToString();
        }


        public bool ISOSave()
        {
            lock (FileReadWriteLock)
            {
                try
                {
                    using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream("vacation_" + UniqueID.ToString() + ".xml", FileMode.Create, isf))
                        {
                            using (StreamWriter sw = new StreamWriter(isfs))
                            {
                                m_oVacationXMLRoot.Save(sw);
                                App.BytesWritten += sw.BaseStream.Length;

                                System.Diagnostics.Debug.WriteLine("SAVED (" + sw.BaseStream.Length.ToString() + " bytes - " + App.BytesWritten.ToString() + "): VACATION ROOT");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("error in XMLModel IsoSave! --- " + ex.Message);
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
                        if (isf.FileExists("vacation_" + UniqueID.ToString() + ".xml"))
                        {
                            using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream("vacation_" + UniqueID.ToString() + ".xml", FileMode.Open, isf))
                            {
                                using (StreamReader sr = new StreamReader(isfs))
                                {
                                    m_oVacationXMLRoot = XElement.Load(sr);
                                    App.BytesRead += sr.BaseStream.Length;

                                    System.Diagnostics.Debug.WriteLine("LOADED (" + sr.BaseStream.Length.ToString() + " bytes - " + App.BytesRead.ToString() + "): VACATION ROOT");
                                }
                            }
                        }
                        else
                        {
                            EmptyVacation();
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("error in XMLModel IsoLoad (depersisting)! --- " + ex.Message);
                    System.Diagnostics.Debug.Assert(false);
                    return false;
                }

                try
                {
                    m_oIconsRoot = m_oVacationXMLRoot.Element("Icons");
                    m_oPropertiesRoot = m_oVacationXMLRoot.Element("Properties");

                    //
                    // create all the IconXMLModels
                    //

                    var icons = from icon in m_oIconsRoot.Elements()
                                where icon.Attributes("UniqueID") != null
                                select new
                                {
                                    uniqueid = icon.Attribute("UniqueID").Value,
                                    item = icon
                                };

                    List<int> lToRet = new List<int>();
                    int n = 0;
                    foreach (var w in icons)
                    {
                        n++;
                        XMLIconModel icm = new XMLIconModel(w.item, this, m_oSaveMgr);
                        m_oIconXMLModels.Add(Convert.ToInt32(w.uniqueid), icm);
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
                            if (isf.FileExists("vacation_" + m_nUniqueID.ToString() + ".xml"))
                                isf.DeleteFile("vacation_" + m_nUniqueID.ToString() + ".xml");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("RESTVacationModel RemoveYourself error: " + ex.Message);
                    System.Diagnostics.Debug.Assert(false);
                    return false;
                }

                return true;
            }
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
            if (m_oTimelineXMLNode.Element(in_strPropertyName) != null)
                return m_oTimelineXMLNode.Element(in_strPropertyName).Value;

            return "";
        }

        public bool PropertyToParent(string in_strPropertyName, string in_strPropertyValue)
        {
            //if (in_strPropertyName == "DockLine" || in_strPropertyName == "IconDuration" || in_strPropertyName == "DockTime")
            {
                if (m_oTimelineXMLNode.Element(in_strPropertyName) == null)
                {
                    // the property isn't there! create it and insertit.
                    m_oTimelineXMLNode.Add(new XElement(in_strPropertyName, in_strPropertyValue));
                }
                else
                {
                    m_oTimelineXMLNode.Element(in_strPropertyName).Value = in_strPropertyValue;
                }
                m_oParentModel.Save();
            }

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

            //PropertyToTimeline(in_strPropertyName, in_strPropertyValue);

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

            if( !in_bNoUndo )
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
                return m_oParentModel as IModel;
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
