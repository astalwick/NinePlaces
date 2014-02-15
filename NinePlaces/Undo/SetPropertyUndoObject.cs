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
using NinePlaces.Models.REST;
using Common.Interfaces;
using Common;

namespace NinePlaces.Undo
{
    /// <summary>
    /// Simple SetProperty undo object.  Sets and unsets a property on
    /// the IHierarchicalProperty object identified by UniqueID.
    /// </summary>
    public class SetPropertyUndoObject : IUndoObject
    {
        /// <summary>
        /// UniqueID of the IHierarchicalProperty object that is the target of 
        /// the undos and redos.
        /// </summary>
        public int UniqueID
        {
            get
            {
                return m_nUniqueID;
            }
            set
            {
                if (m_nUniqueID != value)
                    m_nUniqueID = value;
            }
        }
        protected int m_nUniqueID = -1;
        
        /// <summary>
        /// Property name that is being set/undone/redone.
        /// </summary>
        public string PropertyName
        {
            get
            {
                return m_strPropertyName;
            }
            set
            {
                if (m_strPropertyName != value)
                    m_strPropertyName = value;
            }
        }
        protected string m_strPropertyName = string.Empty;

        /// <summary>
        /// Value being set/undone/redone.
        /// </summary>
        public string PropertyValue
        {
            get
            {
                return m_strPropertyValue;
            }
            set
            {
                if (m_strPropertyValue != value)
                    m_strPropertyValue = value;
            }
        }
        protected string m_strPropertyValue = string.Empty;

        /// <summary>
        /// Previous value - recorded so that the undo object knows
        /// what to undo to.
        /// </summary>
        public string PreviousValue
        {
            get
            {
                return m_strPreviousValue;
            }
            set
            {
                if (m_strPreviousValue != value)
                    m_strPreviousValue = value;
            }
        }
        protected string m_strPreviousValue = string.Empty;

        protected IUndoWrapper m_oUndoWrapper = null;
        public SetPropertyUndoObject()
        {
            // remember our undo wrapper
            m_oUndoWrapper = App.UndoMgr.UndoWrapper;
            // mark the undo wrapper as containing events.
            m_oUndoWrapper.ContainsUndoEvents = true;
        }

        public void Undo()
        {
            Log.WriteLine("UNDO SETPROP: " + m_strPropertyName + " m_strPrev=" + m_strPreviousValue + "  m_strProp=" + m_strPropertyValue, LogEventSeverity.Debug);
            DoUndoRedo(m_strPreviousValue);
        }

        public void Redo()
        {
            Log.WriteLine("REDO SETPROP: " + m_strPropertyName + " m_strPrev=" + m_strPreviousValue + "  m_strProp=" + m_strPropertyValue, LogEventSeverity.Debug);
            DoUndoRedo(m_strPropertyValue);
        }

        protected virtual void DoUndoRedo(string in_strValueToSet)
        {
            // find the model that we should be setting,
            ITimelineElementModel m = RESTTimelineModel.RootTimelineModel.FindDescendentModel(m_nUniqueID) as ITimelineElementModel;
            // and then update it with the undone/redonve property value.
            m.SetProperty(m_strPropertyName, in_strValueToSet);
        }

        public virtual void Do(ITimelineElementModel in_oModel)
        {
            // update the given model, and register the change as an undo event.
            in_oModel.SetProperty(m_strPropertyName, m_strPropertyValue);
            App.UndoMgr.RegisterUndo(this, m_oUndoWrapper);
        }
    }

}
