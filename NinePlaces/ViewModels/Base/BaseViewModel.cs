using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Threading;
using NinePlaces.Models;
using NinePlaces.Undo;
using Common.Interfaces;
using Common;
using System.Windows;

namespace NinePlaces.ViewModels
{
    public class BaseViewModel : IHierarchicalViewModel
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler LoadComplete;
        public event EventHandler LoadError;
        public event EventHandler Removed;

        // timer on which we will do the update to the model.
        protected DispatcherTimer m_oUpdateModelTimer = new DispatcherTimer();
        internal Dictionary<string, SetPropertyUndoObject> m_lUpdates = new Dictionary<string, SetPropertyUndoObject>();

        public BaseViewModel()
        {
            // we group model updates into a 25ms tick timespan.  that way,
            // we're not constantly hassling the model.
            m_oUpdateModelTimer.Interval = new TimeSpan(0, 0, 0, 0, 25);
            m_oUpdateModelTimer.Tick += new EventHandler(m_oUpdateModelTimer_Tick);
        }

        public string AuthToken
        {
            get
            {
                return Model.AuthToken;
            }
        }
        public int UniqueID
        {
            get
            {
                return Model.UniqueID;
            }
        }

        public bool WritePermitted
        {
            get
            {
                return Model != null && Model.WritePermitted;
            }
        }

        public bool ChildrenLoaded
        {
            get;
            set;
        }

        // gotta have a model!
        private ITimelineElementModel m_oModel = null;
        public virtual ITimelineElementModel Model
        {
            get
            {
                return m_oModel;
            }
            internal set
            {
                if (m_oModel != value)
                {
                    if (value != null)
                    {
                        bool bAllowPersist = m_oModel == null ? true : m_oModel.AllowPersist;

                        m_oModel = value;
                        m_oModel.PropertyChanged += new PropertyChangedEventHandler(ModelPropertyChanged);
                        m_oModel.ChildGroupChanged += new PropertyGroupChangedEventHandler(Model_ChildGroupChanged);
                        m_oModel.AllowPersist = bAllowPersist;
                        

                        if (!m_oModel.Loaded)
                        {
                            // ok, the model is not loaded from the server yet.
                            // however, there are certain properties that we know already
                            // (promoted properties).  update them.
                            UpdateBaseProperties();

                            m_oModel.LoadComplete += new EventHandler(Model_LoadComplete);
                            m_oModel.SaveError += new EventHandler(Model_SaveError);
                            m_oModel.SaveComplete += new EventHandler(Model_SaveComplete);
                            m_oModel.LoadError += new EventHandler(Model_LoadError);

                            // enqueue the model for loading from the server.
                            m_oModel.Load();
                        }
                        else
                        {
                            // oh, good news, we're already loaded.
                            // lets just take all of the properties, and update.
                            Dictionary<string,string> d = Model.Properties;
                            foreach (string strProperty in d.Keys)
                            {
                                if (UpdatePropertyFromModel(strProperty))
                                    NotifyPropertyChanged(strProperty);
                            }
                            ChildrenLoaded = true;
                        }
                    }
                    else
                    {
                        // we're clearing out our model, so unregister the events.
                        m_oModel.LoadComplete -= new EventHandler(Model_LoadComplete);
                        m_oModel.SaveError-= new EventHandler(Model_SaveError);
                        m_oModel.SaveComplete -= new EventHandler(Model_SaveComplete);
                        m_oModel.LoadError -= new EventHandler(Model_LoadError);
                        m_oModel.ChildGroupChanged += new PropertyGroupChangedEventHandler(Model_ChildGroupChanged);
                        m_oModel.PropertyChanged -= new PropertyChangedEventHandler(ModelPropertyChanged);
                        m_oModel = null;
                    }
                }
            }
        }

        protected virtual void Model_ChildGroupChanged(object sender, PropertyGroupChangedEventArgs e)
        {
            if (e.Action == PropertyGroupChangedAction.Remove)
            {
                ITimelineElementModel m = e.PropertyContainer as ITimelineElementModel;
                if (ChildRemoved != null)
                    ChildRemoved.Invoke(this, new VMChildChangedEventArgs(VMFromModelID(m.UniqueID), false));
            }
            else if (e.Action == PropertyGroupChangedAction.Add)
            {
                ITimelineElementModel m = e.PropertyContainer as ITimelineElementModel;
                if (ChildAdded != null)
                    ChildAdded.Invoke(this, new VMChildChangedEventArgs(VMFromModelID(m.UniqueID), true));
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        protected virtual IViewModel VMFromModelID(int in_nID)
        {
            throw new NotImplementedException();

        }

        protected virtual void ModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if( UpdatePropertyFromModel(e.PropertyName) )
            {
                // updatepropertyfrommodel will return true/false, depending on whether the update was necessary.
                // if it was (true), then we should notify out.
                Log.WriteLine("Model (" + Model.UniqueID + ") updated: " + e.PropertyName, LogEventSeverity.Verbose);
                NotifyPropertyChanged(e.PropertyName);
            }
        }

        public void DisconnectFromModel()
        {
            Model = null;
        }

        public bool Loaded
        {
            get
            {
                if (Model == null)
                    return false;

                return Model.Loaded;
            }
        }

        public bool HasModel
        {
            get
            {
                if (Model == null)
                    return false;

                return true;
            }
        }

        IHierarchicalViewModel m_oParentViewModel = null;
        public IHierarchicalViewModel ParentViewModel
        {
            get
            {
                return m_oParentViewModel;
            }
            set
            {
                bool bInvokeRemoved = false;
                if (m_oParentViewModel != null && value == null && Removed != null)
                {
                    bInvokeRemoved = true;
                    
                }

                m_oParentViewModel = value;
                if( bInvokeRemoved )
                    Removed(this, new EventArgs() );
            }
        }

        protected virtual void Model_LoadComplete(object sender, EventArgs e)
        {
        }

        protected virtual void Model_LoadError(object sender, EventArgs e)
        {
            if (Model == null)
                return;
            Log.WriteLine("IconViewModel received a LoadError", LogEventSeverity.CriticalError);
        }

        protected virtual void Model_SaveComplete(object sender, EventArgs e)
        {
        }

        protected virtual void Model_SaveError(object sender, EventArgs e)
        {
            if (Model == null)
                return;
            Log.WriteLine("IconViewModel received a SaveError", LogEventSeverity.CriticalError);
        }

        // ok, we're doing a notify, but we're NOT updating the property in
        // the model.  this is used if a property in a child class masks a
        // property in the base class.  (eg, DepartureTime in the flight
        // child is really just a masking property for DockTime.  well, 
        // we need to notify when it has changed, but we don't want duplicate
        // properties persisted).
        //
        //
        // NOTE: possible bug:
        // suuppose someone modifies the DepartureTime property.  great, that
        // will fire a notification for both the DepartureTime property AND the
        // DockTime property, so users subscribed to either will get notified.
        // '
        // however, a modification to DockTime does NOT fire the DepartureTime
        // notification.  that's bad.  means some subscribers to that notification
        // won't get notified!
        //
        //
        public virtual void NotifyPropertyChanged(string in_strPropertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(in_strPropertyName));
        }

        // ok first we update the proeprty in the model
        // (persistable value is a string-based representation of the property - 
        // the caller knows how to marshall that to and from the real thing).
        // after the update, fire notifies.
        public virtual void NotifyPropertyChanged(string in_strPropertyName, string in_strPersistablePropertyValue)
        {
            using (new AutoComplexUndo())
            {
                if (Model != null && ( m_lUpdates.ContainsKey(in_strPropertyName) || !Model.HasProperty( in_strPropertyName ) || Model.GetProperty( in_strPropertyName ) != in_strPersistablePropertyValue ) )
                {
                    if (m_lUpdates.ContainsKey(in_strPropertyName))
                    {
                        m_lUpdates[in_strPropertyName].PropertyValue = in_strPersistablePropertyValue;
                    }
                    else
                    {
                        m_lUpdates.Add(in_strPropertyName, PropertySetter(in_strPropertyName, in_strPersistablePropertyValue));
                    }

                    if( !m_oUpdateModelTimer.IsEnabled )
                        m_oUpdateModelTimer.Start();
                }
            }
            NotifyPropertyChanged(in_strPropertyName);
        }

        public virtual SetPropertyUndoObject PropertySetter(string in_strPropertyName, string in_strPersistablePropertyValue)
        {
            return new SetPropertyUndoObject() { UniqueID = this.UniqueID, PropertyName = in_strPropertyName, PropertyValue = in_strPersistablePropertyValue };
        }

        protected void DoSave()
        {
            if (Model == null)
                return;

            Log.Indent();
            foreach (SetPropertyUndoObject s in m_lUpdates.Values)
            {
                s.PreviousValue = Model.GetProperty(s.PropertyName);
                s.Do(Model);
            }
            Log.Unindent();
            m_lUpdates.Clear();
        }

        void m_oUpdateModelTimer_Tick(object sender, EventArgs e)
        {
            if (!App.Dragging)
            {
                m_oUpdateModelTimer.Stop();
                DoSave();
            }
        }

        protected virtual void UpdateBaseProperties()
        {
            if (Model == null)
                return;
        }

        protected virtual bool UpdatePropertyFromModel(string in_strProperty)
        {
            if (Model == null)
                return false;

            // we have nothing to update, so we return false here - meaning NOTHING CHANGED.
            return false;
        }

        protected void InvokeLoadError(Object sender, EventArgs e)
        {
            if( LoadError != null )
                LoadError.Invoke(sender, e);
        }

        public virtual void RemoveChild(IHierarchicalViewModel in_oChild)
        {
            if (ChildRemoved != null)
                ChildRemoved.Invoke(this, new VMChildChangedEventArgs(in_oChild, false));
        }

        protected void InvokeLoadComplete(Object sender, EventArgs e)
        {
            if( LoadComplete != null )
                LoadComplete.Invoke(sender, e);
        }

        protected void InvokeChildrenLoaded(Object sender, EventArgs e)
        {
            ChildrenLoaded = true;
            if (ChildrenLoadedEvent != null)
                ChildrenLoadedEvent.Invoke(sender, e);
            
        }

        #region IViewModel Members

        public event VMChildChangedEventHandler ChildRemoved;
        public event VMChildChangedEventHandler ChildAdded;
        public event EventHandler ChildrenLoadedEvent;

        public IInterfaceElementWithVM InterfaceElement
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        #endregion
    }
}
