using System.Collections.Generic;
using NinePlaces.Models;
using Common.Interfaces;
using Common;

namespace NinePlaces.Managers
{
    /// <summary>
    /// This is the PROPERTYCHANGED undo item.  It represents a single property change undo event.
    /// </summary>
    internal class PropertyChangedUndoItem
    {
        public PropertyChangedUndoItem(int in_nElementUniqueID, string in_strPropertyName, string in_strPreviousValue, string in_strNewValue)
        {
            UniqueID = in_nElementUniqueID;
            PropertyName = in_strPropertyName;
            PreviousValue = in_strPreviousValue;
            NewValue = in_strNewValue;
        }
        internal int UniqueID { get; set; }
        internal string PropertyName { get; set; }
        internal string PreviousValue { get; set; }
        internal string NewValue { get; set; }
    }

    /// <summary>
    /// This is the ChildchangedUndoItem - it represents either a child add or child remove operation
    /// on an IHierarchicalPropertyModel.
    /// </summary>
    internal class ChildChangedUndoItem
    {
        internal int OwnerUniqueID { get; set; }
        internal int ChildUniqueID { get; set; }
        internal bool Created { get; set; }     // if 'created' is true, the the change was an addition, else a removal.
        internal Dictionary<string, string> PropertiesToSet { get; set;  }
        public ChildChangedUndoItem(int in_nOwnerUniqueID, int in_nChildUniqueID, bool in_bCreated)
        {
            PropertiesToSet = new Dictionary<string, string>();
            OwnerUniqueID = in_nOwnerUniqueID;
            ChildUniqueID = in_nChildUniqueID;
            Created = in_bCreated;
        }
    }
        
    public class UndoMgr
    {
        // this is set when a user has undo'ed up the stack and then sets a new property.
        // that action should wipe out the redo stack.
        private bool m_bPropertyChangeClearsTheStacks = false;
        // maps us from unique id to model.
        private Dictionary<int, IUndoableModel> m_oIDToModel = new Dictionary<int, IUndoableModel>();
        // our current location in the undo stack.
        private int m_nCurrentIndex = 0;
        // this is our childchanged stack
        private List<List<ChildChangedUndoItem>> m_oChildChangedStack = new List<List<ChildChangedUndoItem>>();
        // propertychanged stack.
        private List<List<PropertyChangedUndoItem>> m_oPropertyChangeStack = new List<List<PropertyChangedUndoItem>>();
        // this is our threadsafe lock object.
        private object UndoStackLock = new object();
        // noundo can be set to disable undo tempoararily.
        public bool NoUndo { get; set; }

        public UndoMgr()
        {
            NoUndo = true;
            m_oPropertyChangeStack.Add(new List<PropertyChangedUndoItem>());
            m_oChildChangedStack.Add(new List<ChildChangedUndoItem>());
        }

        public void PropertyChanged(IUndoableModel sender, int in_nElementUniqueID, string in_strPropertyName, string in_strPreviousValue, string in_strNewValue)
        {
            if (NoUndo)
                return;

            lock (UndoStackLock)
            {
                if (m_oIDToModel.ContainsKey(in_nElementUniqueID))
                    m_oIDToModel.Remove(in_nElementUniqueID);
                m_oIDToModel.Add(in_nElementUniqueID, sender);
            }
            PropertyChangedUndoItem i = new PropertyChangedUndoItem(in_nElementUniqueID, in_strPropertyName, in_strPreviousValue, in_strNewValue);
            lock( UndoStackLock )
            {
                //
                // first, if we just undid, then we need to blow away THIS and every other change higher up in the stack
                // (in other words, we just undid and then set a property - that means that we don't get 'redo' anymore).
                //
                if (m_bPropertyChangeClearsTheStacks)
                {
                    ClearFuture();
                }
                m_oPropertyChangeStack[m_nCurrentIndex].Add(i);
            }
        }

        public void ChildChanged(IUndoableModel sender, int in_nOwnerElementID, int in_nChildElementID, bool in_bCreated, Dictionary<string, string> in_dictPropertiesToSet)
        {

            lock (UndoStackLock)
            {
                if (m_oIDToModel.ContainsKey(in_nOwnerElementID))
                    m_oIDToModel.Remove( in_nOwnerElementID );

                m_oIDToModel.Add(in_nOwnerElementID, sender);
            }

            if (NoUndo)
                return;

            ChildChangedUndoItem i = new ChildChangedUndoItem(in_nOwnerElementID, in_nChildElementID, in_bCreated);
            if( in_dictPropertiesToSet != null )
            {
                i.PropertiesToSet = new Dictionary<string,string>();//in_dictPropertiesToSet;
                foreach (string strKey in in_dictPropertiesToSet.Keys)
                {
                    i.PropertiesToSet.Add(strKey, in_dictPropertiesToSet[strKey]);
                }
            }

            lock (UndoStackLock)
            {
                //
                // first, if we just undid, then we need to blow away THIS and every other change higher up in the stack
                // (in other words, we just undid and then set a property - that means that we don't get 'redo' anymore).
                //
                if (m_bPropertyChangeClearsTheStacks)
                {
                    ClearFuture();
                }
                m_oChildChangedStack[m_nCurrentIndex].Add(i);
            }
        }

        public void ChildChanged(IUndoableModel sender, int in_nOwnerElementID, int in_nChildElementID, bool in_bCreated)
        {
            ChildChanged(sender, in_nOwnerElementID, in_nChildElementID, in_bCreated, null);
        }

        /// <summary>
        /// wipes out the redo stack (anything after m_nCurrentIndex)
        /// </summary>
        private void ClearFuture()
        {
            if (NoUndo)
                return;
            lock (UndoStackLock)
            {
                m_oPropertyChangeStack[m_nCurrentIndex].Clear();
                m_oChildChangedStack[m_nCurrentIndex].Clear();

                int nNext = m_nCurrentIndex + 1;
                while (nNext < m_oPropertyChangeStack.Count)
                {
                    m_oPropertyChangeStack.RemoveAt(nNext);
                }

                while (nNext < m_oChildChangedStack.Count)
                {
                    m_oChildChangedStack.RemoveAt(nNext);
                }
                m_bPropertyChangeClearsTheStacks = false;
            }
        }

        /// <summary>
        /// Commits the current index as an undo step.
        /// (that means, basically, that it locks off this list, increments
        /// m_nCurrentIndex, and creates a new list).
        /// </summary>
        public void CommitStep()
        {
            if (NoUndo)
                return;

            lock (UndoStackLock)
            {
                //
                // first, is there something that we actually need to do?
                //
                if ((m_oPropertyChangeStack[m_nCurrentIndex].Count == 0&& m_oChildChangedStack[m_nCurrentIndex].Count == 0) || m_bPropertyChangeClearsTheStacks)
                    return;

                m_oPropertyChangeStack.Add(new List<PropertyChangedUndoItem>());
                m_oChildChangedStack.Add(new List<ChildChangedUndoItem>());
                m_nCurrentIndex++;
                Log.WriteLine("Undo Step: " + m_nCurrentIndex.ToString(), LogEventSeverity.Debug);
            }
        }

        public bool Undo()
        {
            lock (UndoStackLock)
            {
                if (m_nCurrentIndex - 1 < 0)
                    return false;

                NoUndo = true;
                m_nCurrentIndex--;

                //
                // ok, first we CREATE any items that need creating in our childchanged stack.
                // we always create first, then set the properties, then delete items.
                //
                for (int i = m_oChildChangedStack[m_nCurrentIndex].Count - 1; i >= 0; i--)
                {
                    ChildChangedUndoItem c = m_oChildChangedStack[m_nCurrentIndex][i];
                    if (!c.Created)
                    {
                        IHierarchicalModel m = (m_oIDToModel[c.OwnerUniqueID] as IHierarchicalPropertyModel).NewChild(c.PropertiesToSet, c.ChildUniqueID);
                        if (m_oIDToModel.ContainsKey(c.ChildUniqueID))
                            m_oIDToModel.Remove(c.ChildUniqueID);
                        m_oIDToModel.Add(c.ChildUniqueID, m as IUndoableModel);
                        Log.WriteLine("Undo recreated element", LogEventSeverity.Debug);
                    }
                }

                //
                // now, set any properties that need to be changed as a result of this undo operation.
                //
                foreach (PropertyChangedUndoItem i in m_oPropertyChangeStack[m_nCurrentIndex])
                {
                    Log.WriteLine("Undo (" + m_nCurrentIndex + ") changed " + i.PropertyName + " from '" + i.NewValue + "' to '" + i.PreviousValue + "'", LogEventSeverity.Debug);
                    m_oIDToModel[i.UniqueID].UndoRedoSetProperty(i.PropertyName, i.PreviousValue);
                }

                //
                // finally, if any children need to be REMOVED because of this undo operation,
                // we do it at the end.  we do this after the propertychanges to ensure that 
                // there is no crash if undo tries to change a property on a removed child.
                //
                foreach (ChildChangedUndoItem c in m_oChildChangedStack[m_nCurrentIndex])
                {
                    if (c.Created)
                    {
                        IHierarchicalModel m = (m_oIDToModel[c.OwnerUniqueID] as IHierarchicalPropertyModel);
                        m.RemoveChild(m_oIDToModel[c.ChildUniqueID] as IHierarchicalModel);
                        Log.WriteLine("Undo removed element", LogEventSeverity.Debug);
                    }
                }


                m_bPropertyChangeClearsTheStacks = true;
                NoUndo = false;
            }
            return true;
        }

        public bool Redo()
        {
            lock (UndoStackLock)
            {
                if (m_nCurrentIndex + 1 == m_oPropertyChangeStack.Count)
                    return false;

                NoUndo = true;
                foreach (ChildChangedUndoItem c in m_oChildChangedStack[m_nCurrentIndex])
                {
                    if (c.Created)
                    {
                        IHierarchicalModel m = (m_oIDToModel[c.OwnerUniqueID] as IHierarchicalPropertyModel).NewChild(c.PropertiesToSet, c.ChildUniqueID);
                        if (m_oIDToModel.ContainsKey(c.ChildUniqueID))
                            m_oIDToModel.Remove(c.ChildUniqueID);
                        m_oIDToModel.Add(c.ChildUniqueID, m as IUndoableModel);
                        Log.WriteLine("Redo created element", LogEventSeverity.Debug);
                    }
                }

                foreach (PropertyChangedUndoItem i in m_oPropertyChangeStack[m_nCurrentIndex])
                {
                    Log.WriteLine("Redo (" + m_nCurrentIndex + ") changed " + i.PropertyName + " from '" + i.PreviousValue + "' to '" + i.NewValue + "'", LogEventSeverity.Debug);
                    m_oIDToModel[i.UniqueID].UndoRedoSetProperty(i.PropertyName, i.NewValue);
                }

                foreach (ChildChangedUndoItem c in m_oChildChangedStack[m_nCurrentIndex])
                {
                    if (!c.Created)
                    {
                        IHierarchicalModel m = (m_oIDToModel[c.OwnerUniqueID] as IHierarchicalPropertyModel);
                        m.RemoveChild(m_oIDToModel[c.ChildUniqueID] as IHierarchicalPropertyModel);
                        Log.WriteLine("Redo removed element", LogEventSeverity.Debug);
                    }
                }
                m_nCurrentIndex++;
            }
            NoUndo = false;
            return true;
        }
    }
}
