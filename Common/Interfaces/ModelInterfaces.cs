using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Common.Interfaces
{


    /// <summary>
    /// An enum defining the major types in the hierarchy.
    /// </summary>
    public enum PersistenceElements
    {
        Unknown,
        Icon,
        Vacation,
        Timeline,
        Photo,
        List,
        ListItem
    }

    public delegate void AccountCreationEventHandler(object sender, AccountCreationEventArgs args);
    public class AccountCreationEventArgs
    {
        public AccountCreationEventArgs(bool in_bFailed)
        {
            Failed = in_bFailed;
        }

        public bool Failed { get; set; }
    }

    public delegate void AuthenticationEventHandler(object sender, AuthenticationEventArgs args);
    public class AuthenticationEventArgs
    {
        public AuthenticationEventArgs( bool in_bAuthenticated, bool in_bFailed )
        {
            Authenticated = in_bAuthenticated;
            Failed = in_bFailed;
        }

        public bool Authenticated { get; set; }
        public bool Failed { get; set; }
    }

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

    public delegate void PhotoDownloadEventHandler(object sender, PhotoDownloadEventArgs args);
    public class PhotoDownloadEventArgs
    {
        /// <summary>
        /// Blank Constuctor
        /// </summary>
        public PhotoDownloadEventArgs()
        {
        }

        /// <summary>
        /// Contstructor with bits
        /// </summary>
        public PhotoDownloadEventArgs(BitmapImage i)
        {
            DownloadedImage = i;
        }

        public BitmapImage DownloadedImage { get; private set; }
    }

    /// <summary>
    /// Interface for the Authentication model.
    /// </summary>
    public interface ILoginSignupModel 
    {
        /// <summary>
        /// Authenticated or not. 
        /// Note that callers can only set this to false.
        /// </summary>
        bool Authenticated { get; set;  }
        /// <summary>
        /// If this is true, silverlight will try to maintain
        /// auth from session to session
        /// </summary>
        bool StayLoggedIn { get; set; }
        /// <summary>
        /// Username used for login
        /// </summary>
        string UserName { get; set; }
        /// <summary>
        /// Password for login.
        /// </summary>
        string Password { get; set; }
        /// <summary>
        /// User's full name.
        /// </summary>
        string EMail { get; set; }

        string HomeCity { get; set; }
        string HomeCountry { get; set; }
        string HomeProvince { get; set; }
        string HomeTimeZone { get; set; }

        string AuthToken { get; }

        RequestStates RequestState { get; }

        /// <summary>
        /// When username and password are set, this will
        /// cause the model to contact the server and do the
        /// authentication.  A notification will be returned
        /// back on the AuthenticationStatusChanged event.
        /// </summary>
        /// <returns></returns>
        bool Authenticate();
        event AuthenticationEventHandler AuthenticationStatusChanged;

        /// <summary>
        /// When username, password and name are set, CreateAccount
        /// will contact the server to create a new account with 
        /// the given credentials.  Status will be broadcast on the 
        /// AccountCreationStatusChanged event.
        /// </summary>
        void CreateAccount();
        event AccountCreationEventHandler AccountCreationStatusChanged;

        event EventHandler RequestStatusChanged;
    }


    // Summary:
    //     Describes the action that caused a System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged
    //     event.
    public enum PropertyGroupChangedAction
    {
        // Summary:
        //     One or more items were added to the collection.
        Add = 0,
        //
        // Summary:
        //     One or more items were removed from the collection.
        Remove = 1,
        //
        // Summary:
        //     One or more items were replaced in the collection.
        Replace = 2,
        //
        // Summary:
        //     The content of the collection changed dramatically.
        Reset = 4,
    }

    public delegate void PropertyGroupChangedEventHandler(object sender, PropertyGroupChangedEventArgs e);
    public class PropertyGroupChangedEventArgs
    {
        public PropertyGroupChangedEventArgs(string in_strName, PropertyGroupChangedAction in_oAction, IHierarchicalPropertyModel in_pContainer)
        {
            m_strName = in_strName;
            m_Action = in_oAction;
            m_oPropertyContainer = in_pContainer;
        }

        private IHierarchicalPropertyModel m_oPropertyContainer = null;
        public IHierarchicalPropertyModel PropertyContainer
        {
            get
            {
                return m_oPropertyContainer;
            }
        }

        private string m_strName = string.Empty;
        public string Name
        {
            get
            {
                return m_strName;
            }
        }

        private PropertyGroupChangedAction m_Action = PropertyGroupChangedAction.Remove;
        public PropertyGroupChangedAction Action
        {
            get
            {
                return m_Action;
            }
            protected set
            {
                if (m_Action != value)
                    m_Action = value;
            }
        }
    }

    public interface IPropertyModel : INotifyPropertyChanged
    {
        void SetProperty(string in_strName, string in_strValue);
        
        string GetProperty(string in_strName);
        string Name { get; }

        bool HasProperty(string in_strName);

        Dictionary<string, string> Properties { get; set; }
    }

    public interface IHierarchicalPropertyModel : IPropertyModel
    {
        void AddChild(IHierarchicalPropertyModel in_pContainer);
        bool RemoveChild(IHierarchicalPropertyModel in_p);
        Dictionary<string, List<IHierarchicalPropertyModel>> Children { get; }

        IHierarchicalPropertyModel Parent { get; set; }
        IHierarchicalPropertyModel NewChild();

        event PropertyGroupChangedEventHandler ChildGroupChanged;
    }

    public interface ITimelineElementModel : IPersistableModel
    {
        /// <summary>
        /// Every model has a UniqueID.
        /// </summary>
        /// 
        int UniqueID { get; }

        /// <summary>
        /// Overrides the authenticated user.  Allows us to load elements
        /// from another user.
        /// </summary>
        string OverrideUserName { get; set; }
        string OverrideUserID { get; set; } 

        /// <summary>
        /// Returns the NodeType of this mmodel.
        /// </summary>
        PersistenceElements NodeType { get; }

        /// <summary>
        /// Removes (destroys) a child model given only the ID.
        /// </summary>
        bool RemoveChild(int in_nUniqueID);

        ITimelineElementModel NewChild(PersistenceElements in_eNodeType);
        /// <summary>
        /// Constructs and returns a new child model, given a type and a set of properties.
        /// </summary>
        ITimelineElementModel NewChild(PersistenceElements in_eNodeType, Dictionary<string, string> in_dictPropertiesToSet);

        /// <summary>
        /// Constructs and returns a new child model, given a type and a set of properties.
        /// </summary>
        ITimelineElementModel NewChild(PersistenceElements in_eNodeType, Dictionary<string, string> in_dictPropertiesToSet, int in_nUniqueID);

        void SetProperty(string in_strName, string in_strValue, bool in_bNoSave); // -> might not be necessary

        /// <summary>
        /// Returns a model interface given an ID, 
        /// if the given ID is a child of this model.
        /// </summary>
        ITimelineElementModel ChildFromID(int in_nUniqueID);

        /// <summary>
        /// Recurses through all descendant nodes in search
        /// of a model matching the given ID.
        /// </summary>
        ITimelineElementModel FindDescendentModel(int in_nUniqueID);

        bool IsFullyPersisted { get; }
    }

    public interface IPersistableModel : IHierarchicalPropertyModel
    {
        /// <summary>
        /// Enqueues the model for save to server.
        /// </summary>
        /// <returns></returns>
        bool Save();

        /// <summary>
        /// Enqueues the model for load fromm server.
        /// </summary>
        /// <returns></returns>
        bool Load();

        event EventHandler SaveComplete;
        event EventHandler SaveError;
        event EventHandler LoadComplete;
        event EventHandler LoadError;

        /// <summary>
        /// Setting this to false disables persistence.
        /// </summary>
        bool AllowPersist { get; set; }
        bool Loaded { get; }

        bool WritePermitted { get; }
        string AuthToken { get; }
    }

    public interface IListModel : ITimelineElementModel
    {
    }

    public interface IListItemModel : ITimelineElementModel
    {
    }

    public interface IIconModel : IPersistableModel
    {
        /// <summary>
        /// Returns the icon type of the given model.
        /// </summary>
        string IconType { get; }
    }

    public interface IIconContainerModel
    {
        DateTime FirstIconDate { get; }
        DateTime LastIconDate { get; }
    }

    /// <summary>
    /// Private interface for use by SAVE MANAGER ONLY
    /// </summary>
    public interface IPrivatePersistableModel : IPersistableModel
    {
        /// <summary>
        /// Executes the save.  This will be called by the save manager
        /// (and should ONLY ever be called by the save manager).
        /// </summary>
        void DoSave();
        /// <summary>
        /// Executes the load.  This will be called by the save manager
        /// (and should ONLY ever be called by the save manager).
        /// </summary>
        void DoLoad();

        /// <summary>
        /// Uses the javascript-backdoor mechanism to force an emergency
        /// save of the element while we are shutting down the app.
        /// </summary>
        void EmergencySave();

        int RetryCount { get; set; }

    }

    public interface IPrivateUploadableModel
    {
        void DoUpload();
        event EventHandler UploadComplete;
    }

    public interface IPhotoModel : ITimelineElementModel
    {
        /// <summary>
        /// Viewmodel sets this to trigger an upload of the 
        /// photo identified by FileInfo.
        /// </summary>
        FileInfo PhotoInfo { set; }

        /// <summary>
        /// Returns true if the photo is located at the server
        /// </summary>
        bool IsAtServer { get; }
        /// <summary>
        /// Returns the URL to the photo on the server
        /// </summary>
        string PhotoURL { get; }
        /// <summary>
        /// Returns the URL to the thumbnail
        /// </summary>
        string ThumbnailURL { get; }

        /// <summary>
        /// Begins a download of the photo thumbnail.
        /// ThumbnailREady will be invoked when the thumbnail has
        /// downloaded.
        /// </summary>
        void BeginGetThumb();
        event PhotoDownloadEventHandler ThumbnailReady;

        /// <summary>
        /// Begins a download of the full res photo.
        /// PhotoReady will be invoked when the thumbnail has
        /// downloaded.
        /// </summary>
        void BeginGetPhoto();
        event PhotoDownloadEventHandler PhotoReady;

        event EventHandler ProgressChanged;

        int UploadStatus { get; }
        int DownloadStatus { get; }

    }

}
