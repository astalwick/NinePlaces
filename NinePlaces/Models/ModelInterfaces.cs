using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NinePlaces.Models
{
    public delegate void ChildChangedEventHandler(object sender, ChildChangedEventArgs args);
    public class ChildChangedEventArgs
    {
        /// <summary>
        /// Blank Constuctor
        /// </summary>
        public ChildChangedEventArgs()
        {
        }

        /// <summary>
        /// Contstructor with bits
        /// </summary>
        /// <param name="horizontalChange">Horizontal change</param>
        /// <param name="verticalChange">Vertical change</param>
        /// <param name="mouseEventArgs">The mouse event args</param>
        public ChildChangedEventArgs(int in_nOwnerID, int in_nChildID, bool in_bCreated)
        {
            OwnerID = in_nOwnerID;
            ChildID = in_nChildID;
            Created = in_bCreated;
        }

        public int OwnerID { get; set; }
        public int ChildID { get; set; }
        public bool Created { get; set; }
    }

    public interface IUndoableModel : IModel
    {
        bool UndoRedoSetProperty(string in_strPropertyName, string in_strNewValue);
    }

    public interface IModel : IHierarchicalModel, IPersistableModel
    {
        int UniqueID { get; }
    }

    public interface IPersistableModel
    {
        bool ISOSave();         // called by the save manager to actually perform the save
        bool ISOLoad();
        bool DoneLoad();        // called by the save manager when the load is finished.
        bool DoneSave();

        bool RemoveYourself();

        event EventHandler SaveComplete;
        event EventHandler SaveError;
        event EventHandler LoadComplete;
        event EventHandler LoadError;

        bool AllowPersist { get; set; }
        bool DelayPersist { get; set; }
        bool Loaded { get; }

        bool Save();            // tells the model to queue itself up for saving.
        bool Load();

        void EmergencySave();
    }

    public interface IHierarchicalModel : IPropertyModel
    {
        bool PropertyToParent(string in_strPropertyName, string in_strPropertyValue);
        string PropertyFromParent(string in_strPropertyName);
        List<IModel> GetChildModels();
        IModel ParentModel { get; }
        IModel NewChild();
        IModel NewChild(Dictionary<string, string> in_dictPropertiesToSet);
        IModel NewChild(Dictionary<string, string> in_dictPropertiesToSet, int in_nUniqueID);
        bool RemoveChild(IModel in_oModel);

        IModel ChildFromID(int in_nUniqueID);

        event ChildChangedEventHandler ChildAdded;
        event ChildChangedEventHandler ChildRemoved;
    }

    public interface IPropertyModel : INotifyPropertyChanged
    {
        Dictionary<string, string> AllProperties();
        bool UpdateProperty(string in_strPropertyName, string in_strPropertyValue);
        bool UpdateProperty(string in_strPropertyName, string in_strPropertyValue, bool in_bNoNotify, bool in_bNoUndo);
        bool HasProperty(string in_strPropertyName);
        string PropertyValue(string in_strPropertyName);
    }

    public interface IIconContainerModel
    {
        DateTime FirstIconDate { get; }
        DateTime LastIconDate { get; }

    }
}
