using System;
using System.Collections.Generic;
using NinePlaces.Models;
using Common.Interfaces;
using NinePlaces.ViewModels;
using Common;
using NinePlaces.Undo;
using System.IO;
using System.Linq;
using NinePlaces.Helpers;
using System.Collections.ObjectModel;

namespace NinePlaces.ViewModels
{


    public class VacationViewModel : BaseViewModel, IVacationViewModel
    {
        public VacationViewModel(ITimelineElementModel in_oModel)
        {
            Model = in_oModel;
            Lists = new ObservableCollection<IListViewModel>();
        }

        public List<IArrivalLocationProperties> SortedLocations
        {
            get
            {
                return SortedChildren<IArrivalLocationProperties>();
            }
        }

        public new IInterfaceElementWithVM InterfaceElement
        {
            get
            {
                return null;
            }
            set
            {
            }
        }

        public ObservableCollection<IListViewModel> Lists {get;set;}

        private string m_strVacationShortName = null;
        public string VacationShortName
        {
            get
            {
                return m_strVacationShortName;
            }
            set
            {
                m_strVacationShortName = Common.Helpers.GenerateSlug(value, 128);
                NotifyPropertyChanged("VacationShortName", m_strVacationShortName);
            }

        }
        private string m_strTitle = null;
        public string Title
        {
            get
            {
                return m_strTitle;
            }
            set
            {
                m_strTitle = value;
                NotifyPropertyChanged("Title", m_strTitle);
            }
        }

        private DateTime m_dtFirstIconDate = DateTime.MinValue;
        public DateTime FirstIconDate
        {
            get
            {
                if( m_dtFirstIconDate == DateTime.MinValue )
                    return (Model as IIconContainerModel).FirstIconDate;
               return m_dtFirstIconDate;
            }
            internal set
            {
                m_dtFirstIconDate = value;
            }
        }

        private DateTime m_dtLastIconDate = DateTime.MinValue;
        public DateTime LastIconDate
        {
            get
            {
                if (m_dtLastIconDate == DateTime.MinValue)
                    return (Model as IIconContainerModel).LastIconDate;
                return m_dtLastIconDate;
            }
            internal set
            {
                m_dtLastIconDate = value;
            }
        }

        private List<IHierarchicalViewModel> m_IconVMs = new List<IHierarchicalViewModel>();
        public List<IHierarchicalViewModel> Children
        {
            get
            {
                return m_IconVMs;
            }
        }

        public List<T> SortedChildren<T>()
        {
            return (from u in m_IconVMs
                    where (u is IDockableViewModel && u is T)
                    orderby (u as IDockableViewModel).DockTime
                    select (T) u).ToList();  
        }

        public event EventHandler FirstLastIconUpdated;

        protected void UpdateFirstLastIconDate()
        {
            List<IDockableViewModel> v = SortedChildren<IDockableViewModel>();
            
            DateTime dtOldFirst = m_dtFirstIconDate;
            DateTime dtOldLast = m_dtLastIconDate;
            m_dtFirstIconDate = DateTime.MinValue;
            m_dtLastIconDate = DateTime.MinValue;

            if (v.Count == 0)
                return;

            m_dtFirstIconDate = v[0].DockTime;
            m_dtLastIconDate = v[v.Count - 1].DockTime;

            if (FirstLastIconUpdated != null && (dtOldFirst != m_dtFirstIconDate || dtOldLast != m_dtLastIconDate))
                FirstLastIconUpdated.Invoke(this, new EventArgs());
        } 

        void icB_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            IDockableViewModel icvm = sender as IDockableViewModel;
            if (e.PropertyName == "DockTime" && (icvm.DockTime < FirstIconDate || icvm.DockTime > FirstIconDate))
            {
                UpdateFirstLastIconDate();
                UpdateDefaultDepartures();
                UpdateLocations();
            }
            else if (e.PropertyName == "CurrentClass")
            {
                UpdateDefaultDepartures();
                UpdateLocations();
            }
            else if (e.PropertyName == "ArrivalCity")
            {
                UpdateDefaultDepartures();
                UpdateLocations();
            }
        }

        protected void UpdateDefaultDepartures()
        {
            List<IArrivalLocationProperties> v = SortedChildren<IArrivalLocationProperties>();
            for (int i = 1; i < v.Count; i++)
            {
                if (v[i] is IDepartureLocationProperties)
                {
                    IDepartureLocationProperties pDep = v[i] as IDepartureLocationProperties;
                    pDep.DefaultDepartureCity = v[i - 1].ArrivalCity;
                }
            }
        }

        protected void UpdateLocations()
        {
            NotifyPropertyChanged("SortedLocations");
        }

        protected override void Model_ChildGroupChanged(object sender, PropertyGroupChangedEventArgs e)
        {
            if (e.Action == PropertyGroupChangedAction.Remove)
            {
                base.Model_ChildGroupChanged(sender, e);
                ITimelineElementModel m = e.PropertyContainer as ITimelineElementModel;
                IHierarchicalViewModel icB = VMFromModelID(m.UniqueID) as IHierarchicalViewModel;

                if (icB is IListViewModel)
                    Lists.Remove(icB as IListViewModel);

                m_IconVMs.Remove(icB);
                
                icB.DisconnectFromModel();
                icB.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(icB_PropertyChanged);
                icB.ParentViewModel = null;
                NotifyPropertyChanged("SortedLocations");
                
            }
            else if (e.Action == PropertyGroupChangedAction.Add)
            {
                ITimelineElementModel m = e.PropertyContainer as ITimelineElementModel;
                IHierarchicalViewModel vm = _NewVM(Model.ChildFromID(m.UniqueID) as ITimelineElementModel);

                vm.ChildrenLoadedEvent += new EventHandler(vm_ChildrenLoadedEvent);
                UpdateFirstLastIconDate();
                UpdateDefaultDepartures();

                if (vm is IListViewModel)
                    Lists.Add(vm as IListViewModel);

                base.Model_ChildGroupChanged(sender, e);
                NotifyPropertyChanged("SortedLocations");
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        void vm_ChildrenLoadedEvent(object sender, EventArgs e)
        {
            foreach (IHierarchicalViewModel vm in Children)
            {
                if (!vm.ChildrenLoaded)
                    return;
            }

            InvokeChildrenLoaded(this, new EventArgs());
        }


        protected override IViewModel VMFromModelID(int in_nID)
        {
            foreach (IHierarchicalViewModel vm in Children)
            {
                if (in_nID == vm.UniqueID)
                    return vm;
            }
            return null;
        }

        public IPhotoViewModel NewPhoto(DateTime in_dtDockTime, FileInfo in_fiPhotoFile)
        {
            try
            {
                Dictionary<string, string> oPropertiesToSet = new Dictionary<string, string>();
                oPropertiesToSet.Add("DockTime", in_dtDockTime.ToPersistableString());

                CreateChildUndoObject c = new CreateChildUndoObject();
                IPhotoModel im = c.DoCreateChild(PersistenceElements.Photo, Model, oPropertiesToSet) as IPhotoModel;

                im.PhotoInfo = in_fiPhotoFile;

                UpdateFirstLastIconDate();

                return VMFromModelID(im.UniqueID) as IPhotoViewModel;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                return null;
            }
        }

        public IIconViewModel NewIcon(IIconViewModel in_icVM)
        {
            try
            {
                Dictionary<string, string> oPropertiesToSet = new Dictionary<string, string>();
                oPropertiesToSet.Add("IconType", in_icVM.IconType.ToString());
                oPropertiesToSet.Add("DockTime", in_icVM.DockTime.ToPersistableString());
                IMutableIconProperties iMutable = in_icVM as IMutableIconProperties;
                if (iMutable != null)
                    oPropertiesToSet.Add("CurrentClass", iMutable.CurrentClass.ToString());

                CreateChildUndoObject c = new CreateChildUndoObject();
                ITimelineElementModel im = c.DoCreateChild(PersistenceElements.Icon, Model, oPropertiesToSet);
                 
                UpdateFirstLastIconDate();
                UpdateDefaultDepartures();

                IIconViewModel iToRet = VMFromModelID(im.UniqueID) as IIconViewModel;
                (iToRet as IconViewModel).LocalDockTime = (in_icVM as IconViewModel).LocalDockTime;
                return iToRet;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                return null;
            }
        }

        public IListViewModel NewList()
        {
            using (new AutoComplexUndo())
            {
                try
                {
                    Dictionary<string, string> oPropertiesToSet = new Dictionary<string, string>();
                    CreateChildUndoObject c = new CreateChildUndoObject();
                    ITimelineElementModel im = c.DoCreateChild(PersistenceElements.List, Model, oPropertiesToSet);

                    IListViewModel iToRet = VMFromModelID(im.UniqueID) as IListViewModel;
                    return iToRet;
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                    return null;
                }
            }
        }

        protected override void Model_LoadComplete(object sender, EventArgs e)
        {
            base.Model_LoadComplete(sender, e);

            if (Model == null)
                return;

            try
            {
                if (!Model.Children.ContainsKey("Icon") || Model.Children["Icon"] == null || Model.Children["Icon"].Count == 0)
                {
                    InvokeChildrenLoaded(this, new EventArgs());
                }

                InvokeLoadComplete(this, new EventArgs());
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                InvokeLoadError(this, new EventArgs());
            }
        }

        protected IHierarchicalViewModel _NewVM(ITimelineElementModel in_oIconModel)
        {
            try
            {
                IHierarchicalViewModel vm = null;
                if (in_oIconModel is IIconModel)
                {
                    string strIconType = in_oIconModel.GetProperty("IconType");
                    vm = IconRegistry.NewIconVMFromModel(in_oIconModel) as IHierarchicalViewModel;
                }
                else if (in_oIconModel is IListModel)
                {
                    vm = new ListViewModel(in_oIconModel as IListModel);
                }
                else
                {
                    // we should have an interface to check, but for now: if we're not
                    // an icon, we're a photo.
                    vm = new PhotoViewModel(in_oIconModel);
                }
                vm.ParentViewModel = this as IHierarchicalViewModel;
                vm.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(icB_PropertyChanged);

                m_IconVMs.Add(vm);
                return vm;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                return null;
            }
        }

      
        public override void RemoveChild(IHierarchicalViewModel in_oChild)
        {
            try
            {
                using (new AutoComplexUndo())
                {
                    if (in_oChild != null)
                    {
                        CreateChildUndoObject c = new CreateChildUndoObject();
                        c.DoRemoveChild(Model, in_oChild.UniqueID);
                        in_oChild.DisconnectFromModel();
                        UpdateFirstLastIconDate();
                        UpdateDefaultDepartures();
                        m_IconVMs.Remove(in_oChild);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }


            base.RemoveChild(in_oChild);

        }

        protected override bool UpdatePropertyFromModel(string in_strProperty)
        {
            if (Model == null)
                return false;

            if (in_strProperty == "Title" && m_strTitle  != Model.GetProperty("Title"))
            {
                m_strTitle = Model.GetProperty("Title");
                return true;
            }

            if (in_strProperty == "VacationShortName" && m_strVacationShortName != Model.GetProperty("VacationShortName"))
            {
                m_strVacationShortName = Model.GetProperty("VacationShortName");
                return true;
            }

            return base.UpdatePropertyFromModel(in_strProperty);
        }

        
    }
}
