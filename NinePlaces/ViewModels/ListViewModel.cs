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
using Common.Interfaces;
using Common;
using NinePlaces.Undo;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NinePlaces.ViewModels
{
    public class ListViewModel : BaseViewModel, IListViewModel
    {

        public ListViewModel() : base()
        {
            // the BASE class for every list will register to get notified of localization changes.
            Common.CrossAssemblyNotifyContainer.LocalizationChange += new EventHandler(CrossAssemblyNotifyContainer_LocalizationChange);
            ListItems = new ObservableCollection<IListItemViewModel>();
        }

        public ListViewModel(IListModel in_oXMLModel)
            : base()
        {
            Model = in_oXMLModel;
            // the BASE class for every list will register to get notified of localization changes.
            Common.CrossAssemblyNotifyContainer.LocalizationChange += new EventHandler(CrossAssemblyNotifyContainer_LocalizationChange);
            ListItems = new ObservableCollection<IListItemViewModel>();
        }

        private string m_strListName = "";
        public string ListName
        {
            get
            {
                return m_strListName;
            }
            set
            {
                m_strListName = value;
                NotifyPropertyChanged("ListName", m_strListName);
            }
        }

        public ObservableCollection<IListItemViewModel> ListItems { get; set; }

        public IListItemViewModel NewListItem(string in_strEntry, bool in_bChecked)
        {
            using (new AutoComplexUndo())
            {
                try
                {
                    Dictionary<string, string> oPropertiesToSet = new Dictionary<string, string>();
                    oPropertiesToSet.Add("Checked", in_bChecked.ToString());
                    oPropertiesToSet.Add("ListEntry", in_strEntry);
                    CreateChildUndoObject c = new CreateChildUndoObject();
                    ITimelineElementModel im = c.DoCreateChild(PersistenceElements.ListItem, Model, oPropertiesToSet);

                    IListItemViewModel iToRet = VMFromModelID(im.UniqueID) as IListItemViewModel;
                    return iToRet;
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                    return null;
                }
            }

        }

        public virtual IVacationViewModel VacationVM
        {
            get
            {
                return ParentViewModel as IVacationViewModel;
            }
        }

        protected override bool UpdatePropertyFromModel(string in_strProperty)
        {
            if (Model == null)
                return false;

            if (in_strProperty == "ListName" && Model.HasProperty("ListName"))
            {
                m_strListName = Model.GetProperty("ListName");
                return true;
            }
            
            return base.UpdatePropertyFromModel(in_strProperty);
        }

        protected override void UpdateBaseProperties()
        {
            if (Model == null)
                return;

            try
            {
                if (Model.HasProperty("ListName"))
                {
                    m_strListName = Model.GetProperty("ListName");
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                System.Diagnostics.Debug.Assert(false);
            }

            base.UpdateBaseProperties();
        }


        protected override void Model_LoadComplete(object sender, EventArgs e)
        {
            base.Model_LoadComplete(sender, e);

            if (Model == null)
                return;

            try
            {
                if (!Model.Children.ContainsKey("ListItem") || Model.Children["ListItem"] == null || Model.Children["ListItem"].Count == 0)
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

        protected virtual void CrossAssemblyNotifyContainer_LocalizationChange(object sender, EventArgs e)
        {
        }

        protected override void Model_LoadError(object sender, EventArgs e)
        {
            Log.WriteLine("ListViewModel received a LoadError", LogEventSeverity.CriticalError);
            base.Model_LoadError(sender, e);
        }

        protected override void Model_SaveError(object sender, EventArgs e)
        {
            Log.WriteLine("ListViewModel received a SaveError", LogEventSeverity.CriticalError);
            base.Model_SaveError(sender, e);
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


        protected IHierarchicalViewModel _NewVM(ITimelineElementModel in_oModel)
        {
            try
            {
                IHierarchicalViewModel vm = null;
                vm = new ListItemViewModel(in_oModel as IListItemModel) as IHierarchicalViewModel;
                
                vm.ParentViewModel = this as IHierarchicalViewModel;

                m_ItemVMs.Add(vm);
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
                if (in_oChild != null)
                {
                    using (new AutoComplexUndo())
                    {
                        CreateChildUndoObject c = new CreateChildUndoObject();
                        c.DoRemoveChild(Model, in_oChild.UniqueID);
                        in_oChild.DisconnectFromModel();
                        ListItems.Remove(in_oChild as IListItemViewModel);
                        m_ItemVMs.Remove(in_oChild);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }

            base.RemoveChild(in_oChild);
        }


        public IListItemViewModel NewListItem(IListItemViewModel in_icVM)
        {
            try
            {
                Dictionary<string, string> oPropertiesToSet = new Dictionary<string, string>();

                CreateChildUndoObject c = new CreateChildUndoObject();
                ITimelineElementModel im = c.DoCreateChild(PersistenceElements.List, Model, oPropertiesToSet);

                return VMFromModelID(im.UniqueID) as IListItemViewModel;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                return null;
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

        protected override void Model_ChildGroupChanged(object sender, PropertyGroupChangedEventArgs e)
        {
            if (e.Action == PropertyGroupChangedAction.Remove)
            {
                base.Model_ChildGroupChanged(sender, e);
                ITimelineElementModel m = e.PropertyContainer as ITimelineElementModel;
                IHierarchicalViewModel icB = VMFromModelID(m.UniqueID) as IHierarchicalViewModel;

                m_ItemVMs.Remove(icB);
                ListItems.Remove(icB as IListItemViewModel);

                icB.DisconnectFromModel();
                icB.ParentViewModel = null;
            }
            else if (e.Action == PropertyGroupChangedAction.Add)
            {
                ITimelineElementModel m = e.PropertyContainer as ITimelineElementModel;
                IHierarchicalViewModel vm = _NewVM(Model.ChildFromID(m.UniqueID) as ITimelineElementModel);

                ListItems.Add(vm as IListItemViewModel);

                vm.ChildrenLoadedEvent += new EventHandler(vm_ChildrenLoadedEvent);
                base.Model_ChildGroupChanged(sender, e);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private List<IHierarchicalViewModel> m_ItemVMs = new List<IHierarchicalViewModel>();
        public List<IHierarchicalViewModel> Children
        {
            get
            {
                return m_ItemVMs;
            }
        }
    }

}
