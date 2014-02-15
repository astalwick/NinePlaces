using System;
using System.Collections.Generic;
using NinePlaces.Models;
using NinePlaces.Models.REST;
using Common.Interfaces;
using Common;
using NinePlaces.Undo;
using System.Linq;
using NinePlaces.Helpers;
using System.Globalization;
namespace NinePlaces.ViewModels
{


    public class TimelineViewModel : BaseViewModel, ITimelineViewModel
    {
        private bool m_bAllowUpdateToModel = true;
        public bool AllowPersist
        {
            get
            {
                if (Model == null)
                    return m_bAllowUpdateToModel;
                else
                    return Model.AllowPersist;
            }
            set
            {
                m_bAllowUpdateToModel = value;
                if (Model != null)
                {
                    Model.AllowPersist = m_bAllowUpdateToModel;
                }
            }
        }

        public bool IsFullyPersisted
        {
            get
            {
                if (Model == null)
                    return true;

                return Model.IsFullyPersisted;
            }

        }

        private ITimeline m_oTimeline = null;
        public new IInterfaceElementWithVM InterfaceElement
        {
            get
            {
                return m_oTimeline as IInterfaceElementWithVM;
            }
            set
            {
                if (value == null)
                {
                    m_oTimeline = null;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(value is ITimeline);
                    m_oTimeline = value as ITimeline;
                }
            }
        }

        private List<IVacationViewModel> m_VacationVMs = new List<IVacationViewModel>();

        int m_nLastVacationID = 0;
        public int LastVacationUniqueID
        {
            get
            {
                return m_nLastVacationID;
            }
            set
            {
                m_nLastVacationID = value;
                NotifyPropertyChanged("LastVacationUniqueID", m_nLastVacationID.ToString());
            }
        }

        double m_dZoom = 0.0;
        public double TimelineZoom
        {
            get
            {
                return m_dZoom;
            }
            set
            {
                m_dZoom = value;
            }
        }

        DateTime m_dtStartDate = DateTime.Now.AddDays(-3);
        public DateTime TimelineViewStartTime
        {
            get
            {
                return m_dtStartDate;
            }
            set
            {
                m_dtStartDate = value;
            }
        }

        TimeSpan m_tsDuration = new TimeSpan( 36,12,0,0 )  ;    // default to 36.5 days.
        public TimeSpan TimelineViewDuration
        {
            get
            {
                return m_tsDuration;
            }
            set
            {
                m_tsDuration = value;
            }
        }

        public List<IVacationViewModel> VisibleVacations
        {
            get
            {
                return m_VacationVMs;
            }
        }

        public List<IVacationViewModel> Vacations
        {
            get
            {
                return m_VacationVMs;
            }
        }

        public TimelineViewModel()
        {
            App.AssemblyLoadManager.LoadXap("REST_XML_Model.xap", new EventHandler(wcDownloadXap_OpenReadCompleted));
        }

        void wcDownloadXap_OpenReadCompleted(object sender, EventArgs e)
        {
            Model = new RESTTimelineModel() as ITimelineElementModel;
        }
  
        public bool SaveTimeline()
        {
            if (Model == null)
                return false;
            
            Model.Save();
            return true;
        }

        public bool LoadTimeline()
        {
            Model.OverrideUserName = string.Empty;
            Model.Load();
            return true;
        }

        public bool LoadTimeline(string in_strUser)
        {
            Model.OverrideUserName = in_strUser;
            Model.Load();
            return true;
        }

        protected override void Model_SaveComplete(object sender, EventArgs e)
        {
            if (Model == null)
                return;
        }

        protected override void Model_SaveError(object sender, EventArgs e)
        {
            if (Model == null)
                return;
            Log.WriteLine("TimelineViewModel received a SaveError", LogEventSeverity.CriticalError);
        }

        protected override void Model_LoadComplete(object sender, EventArgs e)
        {
            base.Model_LoadComplete(sender, e);

            if (Model == null)
                return;
            try
            {
                if (!Model.Children.ContainsKey("Vacation") || Model.Children["Vacation"] == null || Model.Children["Vacation"].Count == 0)
                {
                    AllChildrenLoaded();
                }

                InvokeLoadComplete(this, new EventArgs());

            }
            catch (Exception ex)
            {
                Log.Exception(ex); 
                InvokeLoadError(this, new EventArgs());
            }
        }

        protected override void Model_LoadError(object sender, EventArgs e)
        {
            if (Model == null)
                return;
            Log.WriteLine("TimelineViewModel received a LoadError", LogEventSeverity.CriticalError);


            App.UndoMgr.ClearUndo();
        }
        
        void ivm_ChildrenLoadedEvent(object sender, EventArgs e)
        {
            foreach( IVacationViewModel iit in Vacations )
            {
                if( !iit.Loaded || !iit.ChildrenLoaded )
                {
                    return;
                }
            }

            AllChildrenLoaded();
        }

        protected void AllChildrenLoaded()
        {
            InvokeChildrenLoaded(this, new EventArgs());
            App.UndoMgr.ClearUndo();
        }

        public override void RemoveChild(IHierarchicalViewModel in_oChild)
        {
            App.AssemblyLoadManager.Requires("REST_XML_Model.xap");
            RemoveVacation(in_oChild as IVacationViewModel);
            if (Vacations.Count == 0 && ParentViewModel != null)
            {
                ParentViewModel.RemoveChild(this as IHierarchicalViewModel);
                ParentViewModel = null;
            }
            base.RemoveChild(in_oChild);
        }

        private bool RemoveVacation(IVacationViewModel in_oVacationVM)
        {
            App.AssemblyLoadManager.Requires("REST_XML_Model.xap");
            try
            {
                if (in_oVacationVM != null)
                {
                    //App.UndoMgr.RegisterUndo(new CreateChildUndoObject(UniqueID, in_oVacationVM.UniqueID, false));
                    CreateChildUndoObject c = new CreateChildUndoObject();
                    c.DoRemoveChild(Model, in_oVacationVM.UniqueID);
                    //if (Model != null)
                    //    Model.RemoveChild(in_oVacationVM.UniqueID);

                    in_oVacationVM.DisconnectFromModel();
                    return Vacations.Remove(in_oVacationVM);
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                return false;
            }
        }

        protected override IViewModel VMFromModelID(int in_nID)
        {
            App.AssemblyLoadManager.Requires("REST_XML_Model.xap");
            foreach (IVacationViewModel vm in Vacations)
            {
                if (in_nID == vm.UniqueID)
                    return vm;
            }

            return null;
        }

        protected IVacationViewModel _NewVacationVM(ITimelineElementModel in_oModel)
        {
            IViewModel vmExisting = VMFromModelID(in_oModel.UniqueID);

            if (vmExisting  != null)
                return vmExisting as IVacationViewModel;

            VacationViewModel icV = new VacationViewModel(in_oModel);
            icV.ParentViewModel = this as IHierarchicalViewModel;

            icV.ChildrenLoadedEvent += new EventHandler(ivm_ChildrenLoadedEvent);

            Vacations.Add(icV as IVacationViewModel);
            return icV as IVacationViewModel;
        }

        protected override void Model_ChildGroupChanged(object sender, PropertyGroupChangedEventArgs e)
        {
            if (e.Action == PropertyGroupChangedAction.Remove)
            {
                base.Model_ChildGroupChanged(sender, e);
                ITimelineElementModel m = e.PropertyContainer as ITimelineElementModel;
                IVacationViewModel icV = VMFromModelID(m.UniqueID) as IVacationViewModel;
                icV.ParentViewModel = null;
                icV.ChildrenLoadedEvent -= new EventHandler(ivm_ChildrenLoadedEvent);

                Vacations.Remove(icV as IVacationViewModel);
            }
            else if (e.Action == PropertyGroupChangedAction.Add)
            {
                ITimelineElementModel m = e.PropertyContainer as ITimelineElementModel;
                IHierarchicalViewModel vm = _NewVacationVM(Model.ChildFromID(m.UniqueID) as ITimelineElementModel);
                vm.ChildrenLoadedEvent += new EventHandler(vm_ChildrenLoadedEvent);

                base.Model_ChildGroupChanged(sender, e);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        void vm_ChildrenLoadedEvent(object sender, EventArgs e)
        {
            foreach (IVacationViewModel vm in Vacations)
            {
                if (!vm.ChildrenLoaded)
                    return ;
            }

            AllChildrenLoaded();
        }
        
        protected override bool UpdatePropertyFromModel(string in_strProperty )
        {
            if (Model == null)
                return false ;

            if( !Model.HasProperty( in_strProperty ) )
                return false;
            
            if (in_strProperty == "LastVacationUniqueID")
            {
                string strValue = Model.GetProperty("LastVacationUniqueID");
                if (!Int32.TryParse(strValue, out m_nLastVacationID))
                    m_nLastVacationID = 0;
                return true;
            }

            return base.UpdatePropertyFromModel(in_strProperty);
        }

        // pulls properties out of the VM ad into the property members.
        // if the property doesn't exist in the VM, we'll create it (we need it).
        
        public IVacationViewModel NewVacation()
        {
            App.AssemblyLoadManager.Requires("REST_XML_Model.xap");

            /*ITimelineElementModel im = Model.NewChild();
            App.UndoMgr.RegisterUndo(new CreateChildUndoObject(UniqueID, im.UniqueID, true));*/

            CreateChildUndoObject c = new CreateChildUndoObject();
            ITimelineElementModel im = c.DoCreateChild(PersistenceElements.Vacation, Model); 
            return VMFromModelID(im.UniqueID) as IVacationViewModel;
        }

    }
}
