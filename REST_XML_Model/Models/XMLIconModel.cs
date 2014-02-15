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
    public class XMLIconModel : IUndoableModel
    {


        public event ChildChangedEventHandler ChildAdded;
        public event ChildChangedEventHandler ChildRemoved;

        public event EventHandler LoadComplete;
        public event EventHandler LoadError;

        public event EventHandler SaveComplete;
        public event EventHandler SaveError;

        private int m_nUniqueID = 0;
        private XElement m_oIconXMLRoot = null;
        private XElement m_oPropertiesRoot = null;
        private XElement m_oVacationXMLNode = null;

        private XMLSaveManager m_oSaveMgr = null;
        private XMLVacationModel m_oParentModel = null;

        private object FileReadWriteLock = new Object();

        public string IconType
        {
            get;
            internal set;
        }

        public bool Loaded
        {
            get;
            internal set;
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

        public DateTime DockTime
        {
            get
            {
                if (!HasProperty("DockTime"))
                {
                    //System.Diagnostics.Debug.Assert(false);
                    return DateTime.Now;
                }

                return new DateTime(Convert.ToInt64(PropertyValue("DockTime")));
            }
        }

        public XMLIconModel(XElement in_xVacationRoot, XMLVacationModel in_oParentModel, XMLSaveManager in_oSaveMgr)
        {
            m_oSaveMgr = in_oSaveMgr;
            m_oVacationXMLNode = in_xVacationRoot;
            UniqueID = Convert.ToInt32(m_oVacationXMLNode.Attribute("UniqueID").Value);
            IconType = m_oVacationXMLNode.Name.ToString();
            
            m_oParentModel = in_oParentModel;
            m_oIconXMLRoot = new XElement(IconType, 
                new XAttribute("UniqueID", m_nUniqueID.ToString()), 
                new XElement("Properties"));
            m_oPropertiesRoot = m_oIconXMLRoot.Element("Properties");
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

        public bool HasProperty(string in_strPropertyName)
        {
            if (!Loaded)
            {
                return PropertyFromParent(in_strPropertyName) != "";
            }

            return m_oPropertiesRoot.Element(in_strPropertyName) != null;
        }



        public string PropertyValue(string in_strPropertyName)
        {
            if (!Loaded)
            {
                return PropertyFromParent(in_strPropertyName);
            }

            return m_oPropertiesRoot.Element(in_strPropertyName) != null ? m_oPropertiesRoot.Element(in_strPropertyName).Value : "";
        }



        public bool ISOSave()
        {
            lock( FileReadWriteLock )
            {
                try
                {
                    using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream("icon_" + m_nUniqueID.ToString() + ".xml", FileMode.Create, isf))
                        {
                            using (StreamWriter sw = new StreamWriter(isfs))
                            {
                                m_oIconXMLRoot.Save(sw);

                                App.BytesWritten += sw.BaseStream.Length;
                                System.Diagnostics.Debug.WriteLine("SAVED (" + sw.BaseStream.Length.ToString() + " bytes - " + App.BytesWritten.ToString() + "): " + IconType + " - " + m_nUniqueID.ToString());
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("IconXMLModel ISOSave error: " + ex.Message);
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
                        if (isf.FileExists("icon_" + m_nUniqueID.ToString() + ".xml"))
                        {
                            using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream("icon_" + m_nUniqueID.ToString() + ".xml", FileMode.Open, isf))
                            {
                                using (StreamReader sr = new StreamReader(isfs))
                                {
                                    m_oIconXMLRoot = XElement.Load(sr);

                                    App.BytesRead += sr.BaseStream.Length;
                                    System.Diagnostics.Debug.WriteLine("LOADED (" + sr.BaseStream.Length.ToString() + " bytes - " + App.BytesRead.ToString() + "): " + IconType + " - " + m_nUniqueID.ToString());
                                }
                            }
                        }
                        else
                        {
                            throw (new Exception("icon_" + m_nUniqueID.ToString() + ".xml does not exist"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("IconXMLModel ISOLoad error: " + ex.Message);
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
                            if (isf.FileExists("icon_" + m_nUniqueID.ToString() + ".xml"))
                                isf.DeleteFile("icon_" + m_nUniqueID.ToString() + ".xml");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("IconXMLModel RemoveYourself error: " + ex.Message);
                    System.Diagnostics.Debug.Assert(false);
                    return false;
                }

                return true;
            }
        }

        public override string ToString()
        {
            return m_oIconXMLRoot.ToString();
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

        #region IModel Members

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

        public string PropertyFromParent(string in_strPropertyName)
        {
            if (m_oVacationXMLNode.Element(in_strPropertyName) != null)
                return m_oVacationXMLNode.Element(in_strPropertyName).Value;

            return "";
        }

        public bool PropertyToParent(string in_strPropertyName, string in_strPropertyValue)
        {
            if (m_oParentModel == null)
                return false;

            if (m_oVacationXMLNode.Element(in_strPropertyName) == null)
            {
                // the property isn't there! create it and insertit.
                m_oVacationXMLNode.Add(new XElement(in_strPropertyName, in_strPropertyValue));
            }
            else
            {
                m_oVacationXMLNode.Element(in_strPropertyName).Value = in_strPropertyValue;
            }
            m_oParentModel.Save();
            return true;
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

            System.Diagnostics.Debug.WriteLine("Property Update (quiet: " + in_bNoNotify.ToString() + ") " + in_strPropertyName);
                 
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
        #endregion


        public IModel ChildFromID(int in_nID)
        {
            throw new NotImplementedException();
        }

        public bool RemoveChild(IModel in_oNewVacation)
        {
            throw new NotImplementedException();
        }

        public IModel NewChild(Dictionary<string, string> in_dictPropertiesToSet)
        {
            throw new NotImplementedException();
        }

        public IModel NewChild(Dictionary<string, string> in_dictPropertiesToSet, int in_nUniqueID)
        {
            throw new NotImplementedException();
        }
        public IModel NewChild()
        {
            throw new NotImplementedException();
        }

        public List<IModel> GetChildModels()
        {
            throw new NotImplementedException();
        }

        public IModel ParentModel
        {
            get
            {
                return m_oParentModel as IModel;
            }
        }


        #region IUndoable Members

        #endregion

        #region IUndoable Members

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

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region IPersistableModel Members


        public void EmergencySave()
        {
            throw new NotImplementedException();
        }

        #endregion
    }

}
