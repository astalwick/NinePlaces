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
using System.Collections.Generic;
using NinePlaces.Models.REST;
using Common.Interfaces;
using Common;

namespace NinePlaces.Undo
{
    /// <summary>
    /// Undo object that handles creation/deletion of ITimelineElementModel.
    /// Will insert/remove the object from the correct place in the hierarchy of objects.
    /// </summary>
    public class CreateChildUndoObject : IUndoObject
    {
        /// <summary>
        /// UniqueID of the ITimelineElementModel object that is the target of 
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

        /// <summary>
        /// UniqueID of the PARENT of the ITimelineElementModel object that is the target of 
        /// the undos and redos.
        /// </summary>
        public int ParentUniqueID
        {
            get
            {
                return m_nParentUniqueID;
            }
            set
            {
                if (m_nParentUniqueID != value)
                    m_nParentUniqueID = value;
            }
        }
        protected int m_nParentUniqueID = -1;

        /// <summary>
        /// Is the event a 'creation' or a 'removal' of the given object.
        /// </summary>
        public bool Create
        {
            get
            {
                return m_bCreate;
            }
            protected set
            {
                if (m_bCreate != value)
                    m_bCreate = value;
            }
        }
        private bool m_bCreate = false;

        /// <summary>
        /// On creation, certain default properties can be set.
        /// Keep track of them here, so that we can recreate them when undoing
        /// or redoing.
        /// </summary>
        private Dictionary<string, string> m_oPropertiesToSet = null;

        // keep track of our undowrapper.
        private IUndoWrapper m_oWrapper = null;
        public CreateChildUndoObject()
        {
            m_oWrapper = App.UndoMgr.UndoWrapper;
            m_oWrapper.ContainsUndoEvents = true;
        }

        /// <summary>
        /// Undo the creation or deletion event.
        /// </summary>
        public void Undo()
        {
            Log.WriteLine("UNDO CREATECHILD: " + m_nUniqueID, LogEventSeverity.Debug);

            DoUndoRedo(!m_bCreate);
        }

        /// <summary>
        /// Redo the creation or deletion event.
        /// </summary>
        public void Redo()
        {
            Log.WriteLine("REDO CREATECHILD: " + m_nUniqueID, LogEventSeverity.Debug);

            DoUndoRedo(m_bCreate);
        }

        /// <summary>
        /// Do the undo/redo event.
        /// Find the parent model, create or remove the child identified by UniqueID.
        /// </summary>
        /// <param name="in_bCreate"></param>
        private void DoUndoRedo(bool in_bCreate)
        {
            // find the parent model, so that we can mess with its children.
            ITimelineElementModel mParent = RESTTimelineModel.RootTimelineModel.FindDescendentModel(m_nParentUniqueID);
            if (mParent == null)
                throw new Exception("UndoRedo could not find parent for undoredo operations");
            
            if (in_bCreate)
            {
                // we're being told to create!  so, 
                // create a new child, setting all of the default properties that it got
                // the first time it was set.
                mParent.NewChild(NodeType, m_oPropertiesToSet, m_nUniqueID);
            }
            else
            {
                // ok, we're removing a child - so, from among the parent model's children,
                // find the object we need to remove.  and then, like, remove it.
                ITimelineElementModel m = mParent.FindDescendentModel(m_nUniqueID);
                mParent.RemoveChild(m);
            }
        }

        /// <summary>
        /// Create a child without setting any special properties on it.
        /// </summary>
        /// <param name="in_oParentModel"></param>
        /// <returns></returns>
        public ITimelineElementModel DoCreateChild(PersistenceElements in_eNodeType, ITimelineElementModel in_oParentModel)
        {
            m_bCreate = true;   // yup, this is a create undo object, not a delete.
            m_nParentUniqueID = in_oParentModel.UniqueID;
            m_oPropertiesToSet = null;
            m_eNodeType = in_eNodeType;

            // Create the child
            ITimelineElementModel im = in_oParentModel.NewChild(NodeType);
            m_nUniqueID = im.UniqueID;

            // register the undo
            App.UndoMgr.RegisterUndo(this, m_oWrapper);

            // return the newly created child.
            return im;
        }

        /// <summary>
        /// Create a child, with a set of default properties.
        /// </summary>
        /// <param name="in_oParentModel"></param>
        /// <param name="in_oPropertiesToSet"></param>
        /// <returns></returns>
        public ITimelineElementModel DoCreateChild(PersistenceElements in_eNodeType, ITimelineElementModel in_oParentModel, Dictionary<string, string> in_oPropertiesToSet)
        {
            m_eNodeType = in_eNodeType;
            m_bCreate = true;   // yup, this is a create undo object, not a delete.
            m_nParentUniqueID = in_oParentModel.UniqueID;
            m_oPropertiesToSet = in_oPropertiesToSet;

            // Create the child
            ITimelineElementModel im = in_oParentModel.NewChild(NodeType, in_oPropertiesToSet);
            m_nUniqueID = im.UniqueID;

            // register the undo
            App.UndoMgr.RegisterUndo(this, m_oWrapper);

            // return the newly created child.
            return im;
        }

        /// <summary>
        /// Remove a child from the given model.
        /// </summary>
        /// <param name="in_oParentModel"></param>
        /// <param name="in_nUniqueIDToRemove"></param>
        public void DoRemoveChild(ITimelineElementModel in_oParentModel, int in_nUniqueIDToRemove)
        {
            m_bCreate = false;      // this is a RemoveChild uundo event.
            m_nUniqueID = in_nUniqueIDToRemove;
            m_nParentUniqueID = in_oParentModel.UniqueID;

            // find the child.
            ITimelineElementModel im = in_oParentModel.ChildFromID(in_nUniqueIDToRemove) as ITimelineElementModel;

            m_eNodeType = im.NodeType;

            // get its propertyset BEFORE we delete it, so that when if we undo this, we can re-set these properties.
            m_oPropertiesToSet = im.Properties;

            // remove the child from its parent model
            in_oParentModel.RemoveChild(in_nUniqueIDToRemove);

            // and register!
            App.UndoMgr.RegisterUndo(this, m_oWrapper);
        }
    }
}
