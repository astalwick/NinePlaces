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
    public interface IUndoObject
    {
        void Undo();
        void Redo();
    }

    public interface IUndoWrapper
    {
        void Dispose();
        bool ContainsUndoEvents { get; set; }
    }

    public class UndoMgr
    {
        private Stack<AutoComplexUndo> m_oRedoStack = new Stack<AutoComplexUndo>();
        private Stack<AutoComplexUndo> m_oUndoStack = new Stack<AutoComplexUndo>();

        // the undo wrapper is an object which is going to absorbe all individual undo events.
        // for now: complexundo, or noundo.
        private IUndoWrapper m_oUndoWrapper = null;
        internal IUndoWrapper UndoWrapper 
        { 
            get 
            { 
                return m_oUndoWrapper as IUndoWrapper; 
            } 
        }

        /// <summary>
        /// Execute the next event on the Undo stack.
        /// </summary>
        public void Undo()
        {
            if (m_oUndoWrapper != null)
                throw new Exception("Cannot execute an undo while an undowrapper is registered");

            if (m_oUndoStack.Count == 0)
                return; // nothing to do!

            using (new NoUndo())
            {
                // pop an autocomplexundo off of the undo stack.
                AutoComplexUndo aToUndo = null;

                do
                {
                    // note that it's POSSIBLE for undo events to be registered
                    // with a zero count.  we'll pop them off the stack and toss 
                    // them aside until we find an undo event with something to do.
                    aToUndo = m_oUndoStack.Pop();
                } while (aToUndo.Count == 0);

                aToUndo.Undo();

                // remember it on the REDO stack.
                m_oRedoStack.Push(aToUndo);
            }
        }

        /// <summary>
        /// Execute the next event on the redo stack.
        /// </summary>
        public void Redo()
        {
            if (m_oUndoWrapper != null)
                throw new Exception("Cannot execute a redo while an undowrapper is registered");

            if (m_oRedoStack.Count == 0)
                return;

            using (new NoUndo())
            {
                // pop an autocomplexundo off of the undo stack.
                AutoComplexUndo aToRedo = m_oRedoStack.Pop();
                aToRedo.Redo();

                // it becomes an undo event now!
                m_oUndoStack.Push(aToRedo);
            }
        }

        /// <summary>
        /// Clear the undo and redo stacks.
        /// </summary>
        public void ClearUndo()
        {
            m_oUndoStack.Clear();
            m_oRedoStack.Clear();
        }

        internal void UnregisterUndoWrapper(IUndoWrapper undoWrapper)
        {
            if (undoWrapper == m_oUndoWrapper)
            {
                // complex undo complete!
                if (undoWrapper is AutoComplexUndo)
                {
                    AutoComplexUndo autoUndo = undoWrapper as AutoComplexUndo;
                    if (autoUndo.ContainsUndoEvents)
                    {
                        // remember it on the undo stack.
                        m_oUndoStack.Push(autoUndo);
                        //Log.WriteLine("Registering complex undo with: " + (autoUndo).Count + " events");
                    }
                    //Log.WriteLine("Unregister AutoComplex");
                    m_oUndoWrapper = null;
                }
                else if (undoWrapper is NoUndo && m_oPreviousComplexUndo != null)
                {
                    m_oUndoWrapper = m_oPreviousComplexUndo;
                    m_oPreviousComplexUndo = null;
                    //Log.WriteLine("Unregister NOUNDO, set autocopmplext back in");
                }
                else
                {
                    // we're just a noundo, with no previous... clear out.
                    m_oUndoWrapper = null;
                    //Log.WriteLine("Unregister NOUNDO");
                }
            }
            else
            {
                //Log.WriteLine("Unregister ignored non-matching undo.  Currently registered:  " + (m_oUndoWrapper is NoUndo ? " NoUndo" : " AutoComplexUndo"));
            }
        }

        IUndoWrapper m_oPreviousComplexUndo = null;
        internal void RegisterUndoWrapper(IUndoWrapper undoWrapper)
        {
            if (m_oUndoWrapper == null)
            {
                m_oUndoWrapper = undoWrapper;
                //Log.WriteLine("Registered " + (m_oUndoWrapper is NoUndo ? " NoUndo" : " AutoComplexUndo"));
                return;
            }

            // no, we already have an undo wrapper. 
            // basically: noundo always wins.

            if (m_oUndoWrapper is NoUndo)
            {
                //Log.WriteLine("Ignored " + (undoWrapper is NoUndo ? " NoUndo" : " AutoComplexUndo") + " - NoUndo already registered");
                return;         // don't get to override a noundo.
            }

            // ok, it's an autocomplex.  if the incoming is autocmplex as well,
            // fuck it.
            if (undoWrapper is AutoComplexUndo)
            {
                //Log.WriteLine("Ignored AutoComplexUndo - AutoComplexUndo already registered");
                return;
            }

            //Log.WriteLine("Registered NoUndo - AUTOCOMPLEXUNDO MOVED TO PREV!");
            // 
            // interesting, m_o is AutoComplex, but incoming is NoUndo.
            //
            // No undo wins!
            m_oPreviousComplexUndo = m_oUndoWrapper;
            m_oUndoWrapper = undoWrapper;
        }

        internal void RegisterUndo(IUndoObject in_oObject, IUndoWrapper in_oWrapper)
        {
            IUndoWrapper bw = null;
            if (in_oWrapper != null)
            {
                // because some events can be deferred, it's possible that a caller may
                // be registering an undo event that has ALREADY BEEN REMOVED as the
                // current undowrapper.  for a wrapper that is already sitting on the
                // undo stack.
                bw = in_oWrapper;
            }
            else
            {
                bw = m_oUndoWrapper;
            }

            if (bw == null)
            {
                //
                // eventually, we should throw an exception here.
                //
                Log.WriteLine("WARNING - RegisterUndo called with no complex undo set.", LogEventSeverity.Warning);
                return;
            }

            if (bw is AutoComplexUndo)
            {
                // we're registering a new undo event.
                // the redo stack is no longer relevant - toss it.
                m_oRedoStack.Clear();
                // add the undo event.
                (bw as AutoComplexUndo).AddUndoObject(in_oObject);
            }
        }

        internal void RegisterUndo(IUndoObject in_oObject)
        {
            RegisterUndo(in_oObject, null);
        }
    }
}
