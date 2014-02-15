using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Controls;
using System;
using Common;
using System.Windows.Data;

namespace NinePlaces
{
    public delegate void SelectionEventHandler(ISelectable sender, SelectionEventArgs args);
    public class SelectionEventArgs
    {
        public SelectionEventArgs()
        {
        }

        public SelectionEventArgs(bool in_bNewSelectionState)
        {
            NewSelectionState = in_bNewSelectionState;
        }

        public bool NewSelectionState
        {
            get;
            set;
        }
    }

    public interface ISelectable
    {
        event SelectionEventHandler SelectionStateChanged;
        bool Selected { get; set; }
    }

    public interface ISelectionMgr
    {
        void ClearSelection();
        void ClearSelection(string in_strGroup);
        void AddToSelectionGroup(string in_strGroup, ISelectable in_oSelectableObject);
        void RemoveFromSelectionGroup(string in_strGroup, ISelectable in_oSelectableObject);

        event EventHandler SelectionCleared;
    }
    
    public class SelectionMgr : ISelectionMgr
    {
        // this is a list of every selectable item in a SelectionGroup.  a deselect on the string group name will cause all items
        // in this group to be deselected.
        private Dictionary<string, List<ISelectable>> m_dictSelectGroups = new Dictionary<string, List<ISelectable>>();
        // this is a list of only those CURRENTLY SELECTED items in a selectiongroup.
        private Dictionary<string, List<ISelectable>> m_dictSelectedItems = new Dictionary<string, List<ISelectable>>();
        // this is just a reverse lookup so that we can figure out which groups a selectable object is subscribed to.
        private Dictionary<ISelectable, List<string>> m_dictReverseLookup = new Dictionary<ISelectable, List<string>>();

        public SelectionMgr()
        {
        }

        public void ClearSelection()
        {
            List<ISelectable> arToDeselect = new List<ISelectable>();
            foreach (List<ISelectable> oListToDeselect in m_dictSelectedItems.Values)
            {
                foreach( ISelectable oToDeselect in oListToDeselect )
                    arToDeselect.Add( oToDeselect );
            }

            foreach (ISelectable o in arToDeselect)
            {
                o.Selected = false;
            }

            // we've cleared global selection (not just a group), so invoke.
            if( SelectionCleared != null )
                SelectionCleared.Invoke(this, new EventArgs());
        }

        public void ClearSelection(string in_strGroup)
        {
            List<ISelectable> arToDeselect = new List<ISelectable>();
            foreach (ISelectable oToDeselect in m_dictSelectedItems[in_strGroup])
                arToDeselect.Add(oToDeselect);

            foreach (ISelectable o in arToDeselect)
            {
                o.Selected = false;
            }
        }
        
        public void AddToSelectionGroup(string in_strGroup, ISelectable in_oSelectableObject)
        {
            if (m_dictReverseLookup.ContainsKey(in_oSelectableObject) && m_dictReverseLookup[in_oSelectableObject].Contains(in_strGroup))
            {
                // we have it already.
                System.Diagnostics.Debug.Assert(false);
                return;
            }

            if (!m_dictReverseLookup.ContainsKey(in_oSelectableObject))
            {
                in_oSelectableObject.SelectionStateChanged += new SelectionEventHandler(SelectableObject_SelectionStateChanged);
                m_dictReverseLookup.Add(in_oSelectableObject, new List<string>());
            }
            m_dictReverseLookup[in_oSelectableObject].Add(in_strGroup);

            if (!m_dictSelectedItems.ContainsKey(in_strGroup))
                m_dictSelectedItems.Add(in_strGroup, new List<ISelectable>());

            if (!m_dictSelectGroups.ContainsKey(in_strGroup))
                m_dictSelectGroups.Add(in_strGroup, new List<ISelectable>());
            m_dictSelectGroups[in_strGroup].Add(in_oSelectableObject);
        }

        private void SelectableObject_SelectionStateChanged(ISelectable sender, SelectionEventArgs e)
        {
            bool bChange = false;

            if (!m_dictReverseLookup.ContainsKey(sender))
            {
                Log.WriteLine("SelectableObject_SelectionStateChanged would have crashed.  Why is this happening?", LogEventSeverity.Error);
                return;
            }

            // first, check if we're *actually* changing state.
            foreach (string strGroup in m_dictReverseLookup[sender])
            {
                if (!m_dictSelectedItems.ContainsKey(strGroup))
                    System.Diagnostics.Debug.Assert(false);

                if (m_dictSelectedItems.ContainsKey(strGroup) && m_dictSelectedItems[strGroup].Contains(sender) != e.NewSelectionState)
                {
                    bChange = true;
                }
            }

            if (bChange)
            {
                // yes, we're changing state.
                if (e.NewSelectionState)
                {
                    // we're being selected!
                    // that means that we should clear all other selections unless teh ctrl-key is being held.
                    // if it's being held, add the selection to the existing selection set.

                    foreach (string strGroup in m_dictReverseLookup[sender])
                    {
                        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.None)
                        {
                            //
                            // we're not holding down control key.
                            // that means that we need to clear all other selected dudes.
                            //
                            List<ISelectable> arToRemove = new List<ISelectable>();
                            foreach (ISelectable o in m_dictSelectedItems[strGroup])
                            {
                                arToRemove.Add(o);
                            }
                            foreach (ISelectable oToRemove in arToRemove)
                            {
                                m_dictSelectedItems[strGroup].Remove(oToRemove);
                                oToRemove.Selected = false;
                            }
                        }

                        System.Diagnostics.Debug.Assert(m_dictSelectGroups[strGroup].Contains(sender));
                        m_dictSelectedItems[strGroup].Add(sender);
                    }
                }
                else
                {
                    // we're being deselected.  nothing really to do here except remove ourself from the list.
                    foreach (string strGroup in m_dictReverseLookup[sender])
                    {
                        System.Diagnostics.Debug.Assert(m_dictSelectGroups[strGroup].Contains(sender));
                        m_dictSelectedItems[strGroup].Remove(sender);
                        (sender as ISelectable).Selected = false;
                    }
                }
            }
        }

        public void RemoveFromSelectionGroup(string in_strGroup, ISelectable in_oSelectableObject)
        {
            if (!m_dictReverseLookup.ContainsKey(in_oSelectableObject) || !m_dictReverseLookup[in_oSelectableObject].Contains(in_strGroup) || !m_dictSelectedItems.ContainsKey(in_strGroup))
            {
                // yeah, no such object in the group.
                return;
            }

            in_oSelectableObject.Selected = false;

            // remove it from the reverse lookup dict
            m_dictReverseLookup[in_oSelectableObject].Remove(in_strGroup);
            if (m_dictReverseLookup[in_oSelectableObject].Count == 0)
            {
                m_dictReverseLookup.Remove(in_oSelectableObject);
            }

            // remove it from the group.
            if (m_dictSelectedItems[in_strGroup].Contains(in_oSelectableObject))
            {
                // make sure to deselect it, though.
                m_dictSelectedItems[in_strGroup].Remove(in_oSelectableObject);

                if (m_dictSelectedItems[in_strGroup].Count == 0)
                    m_dictSelectedItems.Remove(in_strGroup);
            }

            if (m_dictSelectGroups[in_strGroup].Contains(in_oSelectableObject))
            {
                m_dictSelectGroups[in_strGroup].Remove(in_oSelectableObject);

                // remvoe the group if it's empty.
                if (m_dictSelectGroups[in_strGroup].Count == 0)
                    m_dictSelectGroups.Remove(in_strGroup);
            }
        }

        public event EventHandler SelectionCleared;
    }
}
