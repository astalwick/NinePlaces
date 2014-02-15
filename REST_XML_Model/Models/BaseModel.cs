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
using System.Collections.Generic;
using System.ComponentModel;

namespace NinePlaces.Models
{
    public class BaseModel : IHierarchicalPropertyModel
    {
        public event PropertyGroupChangedEventHandler ChildGroupChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public string AuthToken { get { return REST.RESTAuthentication.Auth.AuthToken; } }

        public BaseModel()
        {
        }

        public BaseModel(string in_strName)
        {
            Name = in_strName;
        }

        private IHierarchicalPropertyModel m_oParent = null;
        public IHierarchicalPropertyModel Parent
        {
            get
            {
                return m_oParent;
            }
            set
            {
                m_oParent = value;
            }
        }

        private string m_strName = string.Empty;
        public virtual string Name
        {
            get
            {
                return m_strName;
            }
            protected set
            {
                if (m_strName != value)
                    m_strName = value;
            }
        }

        /// <summary>
        /// may be empty, indicating no children.
        /// </summary>
        private Dictionary<string, string> m_dProps = new Dictionary<string, string>();
        public Dictionary<string, string> Properties
        {
            get
            {
                return m_dProps;
            }
            set
            {
                m_dProps = value;
            }
        }

        /// <summary>
        /// Returns true if this object has a property of the given name.
        /// </summary>
        /// <param name="in_strPropertyName"></param>
        /// <returns></returns>
        public bool HasProperty(string in_strName)
        {
            return Properties.ContainsKey(in_strName);
        }

        /// <summary>
        /// Sets a property on the model.
        /// </summary>
        /// <param name="in_strPropertyName"></param>
        /// <param name="in_strPropertyValue"></param>
        /// <returns></returns>
        public virtual void SetProperty(string in_strName, string in_strValue)
        {
            if (Properties.ContainsKey(in_strName))
                Properties[in_strName] = in_strValue;
            else
                Properties.Add(in_strName, in_strValue);
            
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                InvokePropertyChanged(this, new PropertyChangedEventArgs(in_strName));
            });
        }

        /// <summary>
        /// Returns the value of the requested property, or empty string if it doesn't exist.
        /// </summary>
        /// <param name="in_strPropertyName"></param>
        /// <returns></returns>
        public string GetProperty(string in_strName)
        {
            if( Properties.ContainsKey(in_strName) )
                return  Properties[in_strName];

            return string.Empty;
        }

        private Dictionary<string, List<IHierarchicalPropertyModel>> m_dictPropertyContainers = new Dictionary<string, List<IHierarchicalPropertyModel>>();
        public Dictionary<string, List<IHierarchicalPropertyModel>> Children
        {
            get
            {
                return m_dictPropertyContainers;
            }
            protected set
            {
                if (m_dictPropertyContainers != value)
                    m_dictPropertyContainers = value;

            }
        }

        /// <summary>
        /// Creates a new child model, and sets default properties on the newly created child.
        /// </summary>
        /// <param name="in_eNodeType"></param>
        /// <param name="in_dictPropertiesToSet"></param>
        /// <returns></returns>
        public virtual IHierarchicalPropertyModel NewChild()
        {
            // base subclasses should probably override this.
            IHierarchicalPropertyModel p = new BaseModel();
            p.Parent = this;
            return p;
        }

        public virtual void AddChild(IHierarchicalPropertyModel in_p)
        {
            string in_strGroup = in_p.Name;
            if (!Children.ContainsKey(in_strGroup))
                Children.Add(in_strGroup, new List<IHierarchicalPropertyModel>());

            Children[in_strGroup].Add(in_p);
            in_p.Parent = this;

            InvokeChildChanged(this, new PropertyGroupChangedEventArgs(in_p.Name, PropertyGroupChangedAction.Add, in_p));
        }

        public virtual bool RemoveChild(IHierarchicalPropertyModel in_p)
        {
            string in_strGroup = in_p.Name;
            int nIndex = Children[in_strGroup].IndexOf(in_p);
            Children[in_strGroup].Remove(in_p);

            in_p.Parent = null;

            InvokeChildChanged(this, new PropertyGroupChangedEventArgs(in_p.Name, PropertyGroupChangedAction.Remove, in_p));

            return true;
        }

        protected void InvokePropertyChanged(object sender, PropertyChangedEventArgs p)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(sender, p);
        }
        protected void InvokeChildChanged(object sender, PropertyGroupChangedEventArgs p)
        {
            if (ChildGroupChanged != null)
                ChildGroupChanged(this, p);
        }
    }

    public class TimelineElementBaseModel : BaseModel, IPrivateTimelineElementModel, IPrivatePersistableModel
    {
        /// <summary>
        /// The persistence object type - Icon, Timeline, Vacation
        /// </summary>
        public PersistenceElements NodeType
        {
            get
            {
                return m_eNodeType;
            }
            protected set
            {
                if (m_eNodeType != value)
                    m_eNodeType = value;
            }
        }
        private PersistenceElements m_eNodeType = PersistenceElements.Unknown;


        public virtual bool IsFullyPersisted
        {
            get
            {
                return true;
            }
        }
        public virtual string OverrideUserID
        {
            get;
            set;
        }

        public virtual string OverrideUserName
        {
            get;
            set;
        }

        // random number for unique id creation.
        protected Random r = new Random();

        // dictionary of children from UniqueID to model
        protected Dictionary<int, ITimelineElementModel> m_oChildModelsByUniqueID = new Dictionary<int, ITimelineElementModel>();

        public bool WritePermitted { get; protected set; }
        

        /// <summary>
        /// UniqueID of this persistence element.
        /// </summary>
        public virtual int UniqueID
        {
            get
            {
                return m_nUniqueID;
            }
            set
            {
                m_nUniqueID = value;
            }
        }
        protected int m_nUniqueID = 0;

        public TimelineElementBaseModel()
        {
            WritePermitted = true;
        }

        /// <summary>
        /// Given a UniqueID, returns the model
        /// </summary>
        /// <param name="in_nUniqueID"></param>
        /// <returns></returns>
        public virtual ITimelineElementModel ChildFromID(int in_nID)
        {
            return m_oChildModelsByUniqueID[in_nID] as ITimelineElementModel;
        }

        /// <summary>
        /// Given an ID, this will walk the tree of children below this model to find
        /// a model with the same ID.
        /// 
        /// This could be slow if the tree is deep or very wide.
        /// </summary>
        /// <param name="in_nUniqueID"></param>
        /// <returns></returns>
        public ITimelineElementModel FindDescendentModel(int in_nID)
        {
            if (UniqueID == in_nID)
                return this;

            if (m_oChildModelsByUniqueID.ContainsKey(in_nID))
                return m_oChildModelsByUniqueID[in_nID] as ITimelineElementModel;

            ITimelineElementModel iToRet = null;
            foreach (ITimelineElementModel m in m_oChildModelsByUniqueID.Values)
            {
                iToRet = m.FindDescendentModel(in_nID);
                if (iToRet != null)
                    break;
            }

            return iToRet as ITimelineElementModel;
        }

        #region IPrivatePersistableModel Members

        public virtual void DoSave()
        {
            throw new NotImplementedException();
        }

        public virtual void DoLoad()
        {
            throw new NotImplementedException();
        }

        public virtual void EmergencySave()
        {
            throw new NotImplementedException();
        }

        public int RetryCount { get; set; }

        #endregion

        #region IPersistableModel Members

        public virtual bool Save()
        {
            throw new NotImplementedException();
        }

        public virtual bool Load()
        {
            throw new NotImplementedException();
        }

        public event EventHandler SaveComplete;
        public event EventHandler SaveError;
        public event EventHandler LoadComplete;
        public event EventHandler LoadError;


        protected void InvokeSaveComplete(object sender, EventArgs p)
        {
            if (SaveComplete != null)
                SaveComplete.Invoke(sender, p);
        }
        protected void InvokeSaveError(object sender, EventArgs p)
        {
            if (SaveError != null)
                SaveError.Invoke(sender, p);
        }
        protected void InvokeLoadComplete(object sender, EventArgs p)
        {
            if (LoadComplete != null)
                LoadComplete.Invoke(sender, p);
        }
        protected void InvokeLoadError(object sender, EventArgs p)
        {
            if (LoadError != null)
                LoadError.Invoke(sender, p);
        }

        public virtual bool AllowPersist
        {
            get;
            set;
        }

        public virtual bool Loaded
        {
            get;
            internal set;
        }

        #endregion

        #region ITimelineElementModel Members


        public virtual bool RemoveChild(int in_nUniqueID)
        {
            throw new NotImplementedException();
        }

        public virtual ITimelineElementModel NewChild(PersistenceElements in_eNodeType)
        {
            throw new NotImplementedException();
        }

        public virtual ITimelineElementModel NewChild(PersistenceElements in_eNodeType, Dictionary<string, string> in_dictPropertiesToSet)
        {
            throw new NotImplementedException();
        }

        public virtual ITimelineElementModel NewChild(PersistenceElements in_eNodeType, Dictionary<string, string> in_dictPropertiesToSet, int in_nUniqueID)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region ITimelineElementModel Members
        public virtual void SetProperty(string in_strName, string in_strValue, bool in_bNoSave)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}