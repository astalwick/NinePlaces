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
using Common;

namespace NinePlaces.Undo
{
    /// <summary>
    /// Base class for all undowrapper objects.
    /// </summary>
    public class BaseUndoWrapper : IDisposable, IUndoWrapper
    {
        public BaseUndoWrapper()
        {
            App.UndoMgr.RegisterUndoWrapper(this);
        }

        public bool ContainsUndoEvents { get; set; }

        #region IDisposable Members
        public virtual void Dispose()
        {
            // we've gone out of scope. 
            // however - we're going to register the undo event, so don't
            // garbage collect it just now.  let the garbage collector get it.

            //
            // question: will this leak?
            // this *will* get garbage collected at some point, no?
            //
            System.GC.SuppressFinalize(this);
            App.UndoMgr.UnregisterUndoWrapper(this);
        }
        #endregion
    }

    /// <summary>
    /// Undo wrapper to collect up all individual undo events into a single complex undo
    /// </summary>
    public class AutoComplexUndo : BaseUndoWrapper
    {
        // maintain a list of the individual undo events!
        private List<IUndoObject> m_oUndoStack = new List<IUndoObject>();
        public AutoComplexUndo()
        {
        }

        /// <summary>
        /// Take the list of undo objects as the occurred, reverse the list,
        /// and tell the object to do its undo action.
        /// </summary>
        public void Undo()
        {
            // we'll clone the list before we reverse it,
            // so that our master list remains untouched.
            List<IUndoObject> oReverseUndoOps = new List<IUndoObject>();
            oReverseUndoOps.AddRange(m_oUndoStack);
            oReverseUndoOps.Reverse();
            foreach (IUndoObject oToUndo in oReverseUndoOps)
            {
                oToUndo.Undo();
            }
        }

        /// <summary>
        /// Take the list of redo objects, and have each object redo
        /// itself.
        /// </summary>
        public void Redo()
        {
            foreach (IUndoObject oToRedo in m_oUndoStack)
            {
                oToRedo.Redo();
            }
        }

        /// <summary>
        /// Number of individual undo events contained in this complex undo.
        /// </summary>
        public int Count
        {
            get
            {
                return m_oUndoStack.Count;
            }
        }

        /// <summary>
        /// Add another undo event to this complex undo.
        /// </summary>
        /// <param name="in_oObject"></param>
        public void AddUndoObject(IUndoObject in_oObject)
        {
            m_oUndoStack.Add(in_oObject);
        }

        #region IDisposable Members

        public override void Dispose()
        {
            base.Dispose();
        }

        #endregion
    }
}
