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

namespace NinePlaces.ViewModels
{
    public class ListItemViewModel : BaseViewModel, IListItemViewModel
    {
        public ListItemViewModel()
        {
            Common.CrossAssemblyNotifyContainer.LocalizationChange += new EventHandler(CrossAssemblyNotifyContainer_LocalizationChange);
        }
        public ListItemViewModel(IListItemModel in_oModel)
        {
            Common.CrossAssemblyNotifyContainer.LocalizationChange += new EventHandler(CrossAssemblyNotifyContainer_LocalizationChange);
            Model = in_oModel as ITimelineElementModel;
        }

        public virtual IListViewModel ListVM
        {
            get
            {
                return ParentViewModel as IListViewModel;
            }
        }

        protected virtual void CrossAssemblyNotifyContainer_LocalizationChange(object sender, EventArgs e)
        {
        }

        protected override void Model_LoadError(object sender, EventArgs e)
        {
            Log.WriteLine("IconViewModel received a LoadError", LogEventSeverity.CriticalError);
            base.Model_LoadError(sender, e);
        }

        protected override void Model_SaveError(object sender, EventArgs e)
        {
            Log.WriteLine("IconViewModel received a SaveError", LogEventSeverity.CriticalError);
            base.Model_SaveError(sender, e);
        }

        private int m_nOrder = -1;
        public int Order
        {
            get
            {
                return m_nOrder;
            }
            set
            {
                if (m_nOrder != value)
                {
                    m_nOrder = value;
                    NotifyPropertyChanged("Order", m_nOrder.ToString());
                }
            }

        }

        private string m_strListEntry = "";
        public string ListEntry
        {
            get
            {
                return m_strListEntry;
            }
            set
            {
                m_strListEntry = value;
                NotifyPropertyChanged("ListEntry", m_strListEntry);
            }
        }

        private bool m_bChecked = false;
        public bool Checked
        {
            get
            {
                return m_bChecked;
            }
            set
            {
                m_bChecked = value;
                NotifyPropertyChanged("Checked", m_bChecked.ToString());
            }
        }

        protected override bool UpdatePropertyFromModel(string in_strProperty)
        {
            if (Model == null)
                return false;

            if (in_strProperty == "ListEntry" && m_strListEntry != Model.GetProperty("ListEntry"))
            {
                m_strListEntry = Model.GetProperty("ListEntry");
                return true;
            }


            if (in_strProperty == "Checked" && m_bChecked.ToString() != Model.GetProperty("Checked"))
            {
                m_bChecked = Convert.ToBoolean( Model.GetProperty("Checked") );
                return true;
            }

            return base.UpdatePropertyFromModel(in_strProperty) ;
        }

        protected override void UpdateBaseProperties()
        {
            if (Model == null)
                return;

            try
            {
                if (Model.HasProperty("ListEntry"))
                {
                    m_strListEntry = Model.GetProperty("ListEntry");
                }

                if (Model.HasProperty("Checked"))
                {
                    m_bChecked = Convert.ToBoolean(Model.GetProperty("Checked"));
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

            InvokeLoadComplete(this, new EventArgs());
            InvokeChildrenLoaded(this, new EventArgs());
        }

        public ICommand RemoveListItem
        {
            get { return new RemoveListItemCommand(this) as ICommand; }
        }
    }


    public class RemoveListItemCommand : ICommand
    {
        IListItemViewModel pViewModel = null;
        public RemoveListItemCommand(IListItemViewModel in_p)
        {
            pViewModel = in_p;
        }
        #region ICommand Members

        public bool CanExecute(object parameter)
        {
            return pViewModel.ParentViewModel != null;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            pViewModel.ParentViewModel.RemoveChild(pViewModel);
        }

        #endregion
    }
}
