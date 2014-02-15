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
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.IO;
using System.Collections.ObjectModel;

namespace Common.Interfaces
{
    public enum RequestStates
    {
        Idle,
        Requesting,
        Requesting_Slow,
        Retrying,
        Failed
    }

    public enum IconTypes
    {
        Undefined = 0,
        Flight,
        Car,
        Hotel,
        Outdoor,
        Photos,
        Restaurant,
        Show,
        Sightseeing,
        MeetUp,
        Camping,
        Boat,
        Beach,
        House,
        Shopping,
        GenericActivity,
        Nightlife,
        SportingEvent,
        Train,
        Bus
    };

    public enum IconClass
    {
        Undefined = 0x0000,
        Transportation = 0x0001,
        Lodging = 0x0002,
        Activity = 0x0004,
        Photo = 0x0008,
        IconStack = 0x0010,

        GenericActivity = Transportation | Lodging | Activity,
    }

    public interface IApp
    {
        UIElement PageRoot { get; }
    }

    /// <summary>
    /// Base details editor interface
    /// </summary>
    public interface IDetailsEditor
    {
        void Show();
        void Hide();
    }

    public interface ILocationObject
    {
        void Copy(ILocationObject in_oObjectToCopy);
        event PropertyChangedEventHandler PropertyChanged;

        string Name { get; set; }
        string CityName { get; set; }
        string Country { get; set; }
        string Province { get; set; }
        string PostalCode { get; set; }
        string StreetAddress { get; set; }
        string TimeZoneID { get; set; }

        bool IsEmpty();
    }

    /// <summary>
    /// Dockable element viewmodel interface.
    /// </summary>
    public interface IDockableViewModel : IHierarchicalViewModel    // interface for elements that are dockable.
    {
        DateTime DockTime { get; set; }
        DateTime LocalDockTime { get; set; }
        bool LocalDockTimeUnreachable { get; }
    }

    /// <summary>
    /// Viewmodel interface for login element
    /// </summary>
    public interface ILoginViewModel
    {
        /// <summary>
        /// The user's email address
        /// </summary>
        string EMail { get; set; }
        string AuthToken { get; }
        bool Authenticated { get; }
        bool StayLoggedIn { get; set; }
        string UserName { get; set; }
        string Password { set; }
        ILocationObject Location { get; set; }

        void Authenticate();
        void ClearAuthentication();
        void CreateAccount();

        RequestStates RequestState { get; }

        event AuthenticationEventHandler AuthenticationStatusChanged;
        event AccountCreationEventHandler AccountCreationStatusChanged;
        event EventHandler RequestStatusChanged;
    }

    /// <summary>
    /// Photo view model.
    /// </summary>
    public interface IPhotoViewModel : IDockableViewModel
    {
        /// <summary>
        /// IsPhotoRes returns true if the photores is available
        /// </summary>
        bool IsPhotoRes { get; }
        bool IsThumbnailAvailable { get; }

        /// <summary>
        /// IsUploaded returns true if the photo has been successfully 
        /// uploaded to the server.
        /// </summary>
        bool IsUploaded { get; }

        /// <summary>
        /// The photo itself!
        /// </summary>
        BitmapImage Photo {get;}

        /// <summary>
        /// Upload progress
        /// </summary>
        int UploadStatus { get; }

        /// <summary>
        /// Download progress
        /// </summary>
        int DownloadStatus { get; }

        /// <summary>
        /// Tells the viewmodel to begin download of the
        /// full photo res.
        /// </summary>
        void DownloadPhoto();
    }

    public interface IIconViewModel : IDockableViewModel  // all icons are inherently hierarchical and dockable
    {
        IconTypes IconType { get; }
        string IconDescription { get; }
    }

    // for Icons that can change their own class
    public interface IMutableIconProperties
    {
        IconClass CurrentClass { get; set; }
        event EventHandler CurrentClassChanged;
    }

    public interface IDurationViewModel     // interface for elements that expose a duration.
    {
        TimeSpan Duration { get; set; }
        DateTime EndTime { get; set; }
    }

    public interface IDurationIconViewModel : IIconViewModel, IDurationViewModel
    {
        bool DurationEnabled { get; set; }
        bool StickyEndTime { get; set; }
    }

    public interface IListViewModel : IHierarchicalViewModel
    {
        string ListName { get; set; }

        IListItemViewModel NewListItem(string in_strEntry, bool in_bChecked);

        ObservableCollection<IListItemViewModel> ListItems { get; set; }
    }

    public interface IListItemViewModel : IHierarchicalViewModel
    {
        string ListEntry { get; set; }
        bool Checked { get; set; }

        int Order { get; set; }

        ICommand RemoveListItem { get; }
    }

    public interface IVacationViewModel : IHierarchicalViewModel
    {
        string Title { get; set; }
        string VacationShortName { get; set; }
        DateTime FirstIconDate { get; }
        DateTime LastIconDate { get; }

        List<IHierarchicalViewModel> Children { get; }
        List<T> SortedChildren<T>();

        ObservableCollection<IListViewModel> Lists { get; set; }

        event EventHandler FirstLastIconUpdated;
        IIconViewModel NewIcon(IIconViewModel in_ic);
        IListViewModel NewList();
        IPhotoViewModel NewPhoto(DateTime in_dtDockTime, FileInfo in_sPhotoFile);
    }

    public interface ITimelineViewModel : IHierarchicalViewModel
    {
        bool AllowPersist { get; set; }
        DateTime TimelineViewStartTime { get; set; }
        TimeSpan TimelineViewDuration { get; set; }
        double TimelineZoom { get; set; }

        List<IVacationViewModel> VisibleVacations { get; }
        List<IVacationViewModel> Vacations { get; }

        bool SaveTimeline();
        bool LoadTimeline();
        bool LoadTimeline(string in_strUserID);

        int LastVacationUniqueID { get; set; }

        IVacationViewModel NewVacation();

        bool IsFullyPersisted { get; }
    }

    public interface IViewModel : INotifyPropertyChanged
    {
        //
        // sets the Model to NULL
        void DisconnectFromModel();

        int UniqueID { get; }

        // returns true if a model is attached (loaded or not).
        bool HasModel { get; }

        // returns true if Model is FULLY LOADED (no more persistence requests required)
        bool Loaded { get; }
        event EventHandler LoadComplete;
        event EventHandler LoadError;

        string AuthToken { get; }
        bool WritePermitted { get; }
    }

    public interface IHierarchicalViewModel : IViewModel
    {
        IHierarchicalViewModel ParentViewModel { get; set; }
        void RemoveChild(IHierarchicalViewModel in_oChild);
        event VMChildChangedEventHandler ChildRemoved;
        event VMChildChangedEventHandler ChildAdded;
        event EventHandler ChildrenLoadedEvent;
        bool ChildrenLoaded { get; }
        event EventHandler Removed;
    }

    public delegate void VMChildChangedEventHandler(object sender, VMChildChangedEventArgs args);
    public class VMChildChangedEventArgs
    {
        /// <summary>
        /// Blank Constuctor
        /// </summary>
        public VMChildChangedEventArgs()
        {
        }

        /// <summary>
        /// Contstructor with bits
        /// </summary>
        /// <param name="horizontalChange">Horizontal change</param>
        /// <param name="verticalChange">Vertical change</param>
        /// <param name="mouseEventArgs">The mouse event args</param>
        public VMChildChangedEventArgs(IViewModel in_oVM, bool in_bCreated)
        {
            ChildVM = in_oVM;
            Created = in_bCreated;
        }

        public IViewModel ChildVM { get; set; }
        public bool Created { get; set; }
    }

    public interface IInterfaceElement
    {
        Control Control { get; }
    }

    public interface IInterfaceElementWithVM : IInterfaceElement
    {
        IViewModel VM { get; set; }
        void Disconnect();
    }


    public interface IArrivalLocationProperties
    {
       // ILocationObject ArrivalObject { get; set; }
        string ArrivalCity { get; set; }
        string ArrivalCountry { get; set; }
        string ArrivalProvince { get; set; }
        string ArrivalPostalCode { get; set; }
        string ArrivalStreetAddress { get; set; }
        DateTime ArrivalTime { get; set; }

    }

    /// <summary>
    /// ITimeZoneSegment represents a single contiguous block
    /// of time that shares a timezone.
    /// </summary>
    public interface ITimeZoneSegment
    {
        /// <summary>
        /// The datetime at which the transition into this
        /// timezone segment begins.  The time between
        /// TransitionStart and Start is 'undefined' - 
        /// there is no local time or timezone associated.
        /// </summary>
        new DateTime TransitionStart { get; }

        /// <summary>
        /// The datetime at which this timezone segment begins
        /// </summary>
        new DateTime Start { get; }

        /// <summary>
        /// Returns the currently selected timezone object.
        /// </summary>
        ITimeZone TimeZoneObj { get; set; }

        /// <summary>
        /// The datetime at which this timezone segment ends.
        /// Note that this is effectively the 'TransitionStart'
        /// of the next timezone segment.
        /// </summary>
        DateTime End { get; } 

        /// <summary>
        /// Retrieves the next timezone segment,
        /// or null if there is none.
        /// </summary>
        ITimeZoneSegment NextSeg { get; set; }

        /// <summary>
        /// Retrieves the previous timezone segment,
        /// or null if there is none.
        /// </summary>
        ITimeZoneSegment PrevSeg { get; set; }

        /// <summary>
        /// The icon that is the 'owner' of this
        /// segment.  The icon that is currently in
        /// charge of driving this timezone's start time.
        /// </summary>
        ITimeZoneDriver SegmentDriver { get; }

        /// <summary>
        /// Returns a list of all 'drivers' - all icons with
        /// the capability of changing/setting timezone info.
        /// </summary>
        List<ITimeZoneDriver> AllDrivers { get; }

        void AddDriver(ITimeZoneDriver in_tz);
        void RemoveDriver(ITimeZoneDriver in_tz);

        /// <summary>
        /// Notifies when anything innteresting happens : this segment changes,
        /// this segment's next/prev seg changes, etc.
        /// </summary>
        event SegmentChangedEventHandler TimeZoneSegmentChanged;
    }

    public delegate void SegmentChangedEventHandler(object sender, SegmentChangedEventArgs args);
    public class SegmentChangedEventArgs
    {
        public SegmentChangedEventArgs(bool in_bZoneChanged, bool in_bPropertiesChanged)
        {
            ZoneChanged = in_bZoneChanged;
            PropertiesChanged = in_bPropertiesChanged;
        }

        public bool ZoneChanged { get; set; }
        public bool PropertiesChanged { get; set; }
    }

    public enum DSTChangeDirection
    {
        DSTJumpForward,
        DSTNone,
        DSTJumpBackward
    }
     
    /// <summary>
    /// The abstract object that represents a single timezone,
    /// eg "America/Montreal".  This object is where all of the info
    /// about offset comes from.
    /// </summary>
    public interface ITimeZone
    {
        bool Loaded { get; }
        event EventHandler TimeZoneInfoUpdated;

        double DSTOffset { get; }
        double Offset { get; }
        string TimeZoneID { get;  }

        double OffsetAtTime(DateTime in_dtTime, bool in_bLocal = false);
        DSTChangeDirection ContainsDSTChange(DateTime in_dtStartTime, DateTime in_dtEndTime, bool in_bLocal = false);

        bool TimeZoneEquals( ITimeZone in_Zone );
    }

    public interface ITimeZoneManager
    {
        void UnregisterIconVM(ITimeZoneSlave in_iIconViewModel);
        void RegisterIconVM(ITimeZoneSlave in_iIconViewModel);
        event TimeZonesChangedEventHandler TimeZonesChanged;
        IList<ITimeZoneSegment> AllSegments { get; }
        double OffsetAtTime(DateTime in_dtTime, bool in_bLocal = false);
        ITimeZoneDriver DefaultTimeZone { get; set; }
        bool NotifyOnZoneChange { get; set; }

    }

    /// <summary>
    /// The interface for on object that is capable of modifying
    /// its timezone.  Eg, any kind of travel icon.
    /// </summary>
    public interface ITimeZoneDriver : ITimeZoneSlave
    {
        /// <summary>
        /// Gets the current timezone for this object.
        /// Note that SETTING the timezoneid is how this object's
        /// timezone can change.  Set "America/Montreal" in will
        /// change this object's timezone.
        /// </summary>
        string TimeZoneID { get; set; }

        /// <summary>
        /// Returns the currently selected timezone object.
        /// </summary>
        ITimeZone TimeZone {get;}

        /// <summary>
        /// The datetime at which the transition into this
        /// timezone segment begins.  The time between
        /// TransitionStart and Start is 'undefined' - 
        /// there is no local time or timezone associated.
        /// </summary>
        DateTime TransitionStart { get; }

        /// <summary>
        /// The datetime at which this timezone segment begins
        /// </summary>
        DateTime Start { get; }

        /// <summary>
        /// Invokes on change of timezone.
        /// </summary>
        event EventHandler TimeZonePropertiesChanged;
    }

    /// <summary>
    /// The interface for an object that has *a state* in which it
    /// can modify its timezone.  A 'generic' icon, when it is in
    /// the travel state, can mmodify the timezone.  When it is in
    /// 'activity' or 'lodging' state, it cannot.
    /// </summary>
    public interface IMutableTimeZoneProperties : ITimeZoneDriver
    {
        /// <summary>
        /// Returns true if the object is currently exposing its timezone.
        /// </summary>
        bool ExposesTimeZone{get;}

        /// <summary>
        /// Invokes if exposestimezone changes...
        /// </summary>
        event EventHandler ExposesTimeZoneChanged;
    }

    public interface ITimeZoneSlave 
    {
        double CurrentOffset { get; }
        ITimeZoneSegment CurrentTimeZoneMasterSegment { get; set; }
        DateTime DockTime { get; }
    }

    public interface IDurationProperties
    {
    }

    public interface IMultiStopProperties : IDepartureLocationProperties, IArrivalLocationProperties
    {
    }

    public interface IDepartureLocationProperties
    {
       // ILocationObject DepartureObject {get;set;}
        string DefaultDepartureCity { get; set; }
        string DepartureCity { get; set; }
        string DepartureCountry { get; set; }
        string DepartureProvince { get; set; }
        string DeparturePostalCode { get; set; }
        string DepartureStreetAddress { get; set; }
        DateTime DepartureTime { get; set; }
    }

    public interface ILocationProperties
    {
        //ILocationObject LocationObject { get; set; }
        string City { get; set; }
        string Country { get; set; }
        string Province { get; set; }
        string PostalCode { get; set; }
        string StreetAddress { get; set; }
    }

    public interface IWithPersonProperties
    {
        string Person { get; set; }
    }
    
    public interface INamedLodgingProperties
    {
        string LodgingName { get; set; }
    }

    public interface INamedActivityProperties
    {
        string ActivityName { get; set; }
    }

    public interface INamedLocationProperties
    {
        string DestinationName { get; set; }
    }

    public interface IBasicProperties
    {

        DateTime DockTime { get; set; }
        string IconDescription { get; }
    }


    public delegate void TimeZonesChangedEventHandler(object sender, TimeZonesChangedEventArgs args);
    public class TimeZonesChangedEventArgs
    {
        /// <summary>
        /// The order of the timezones have changed.
        /// </summary>
        public bool OrderChanged { get; set; }

        /// <summary>
        /// The number of timezones have changed
        /// </summary>
        public bool CountChanged { get; set; }

        /// <summary>
        /// The start/end times of one or more timezones have changed
        /// (see AffectedZones for those timezones that have been
        /// affected).
        /// </summary>
        public bool StartEndTimesChanged { get; set; }

        /// <summary>
        /// List of zones affected by start/end time changes.
        /// </summary>
        public List<ITimeZoneSegment> AffectedZones { get; set; }

        /// <summary>
        /// Contstructor with bits
        /// </summary>
        /// <param name="horizontalChange">Horizontal change</param>
        /// <param name="verticalChange">Vertical change</param>
        /// <param name="mouseEventArgs">The mouse event args</param>
        public TimeZonesChangedEventArgs()
        {
            AffectedZones = new List<ITimeZoneSegment>();
        }
    }

}
