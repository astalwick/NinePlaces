using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Common;
using Common.Interfaces;
using System.Windows.Controls.Primitives;

namespace Editors
{
	public partial class ListsDialog : UserControl
	{
        private IVacationViewModel m_oVacation = null;
        public IVacationViewModel ActiveVacation
        {
            get
            {
                return m_oVacation;
            }

            private set
            {
                m_oVacation = value;
                DataContext = m_oVacation;
                if (m_oVacation.Lists.Count > 0)
                    ListSelectDropDown.SelectedIndex = 0;
                UpdateButtons();
            }
        }

        public event EventHandler Close;
		public ListsDialog(IVacationViewModel in_oActiveVacation)
		{
            
			// Required to initialize variables
			InitializeComponent();
            KeyDown += new KeyEventHandler(ListsDialog_KeyDown);
            Loaded += new RoutedEventHandler(ListsDialog_Loaded);
            LostFocus += new RoutedEventHandler(ListsDialog_LostFocus);

            NewListButton.Click += new RoutedEventHandler(NewListButton_Click);
            RenameListButton.Click += new RoutedEventHandler(RenameListButton_Click);
            DeleteListButton.Click += new RoutedEventHandler(DeleteListButton_Click);
            DoneRenameButton.Click += new RoutedEventHandler(DoneRenameButton_Click);
            ListSelectDropDown.SelectionChanged += new SelectionChangedEventHandler(ListSelectDropDown_SelectionChanged);
            CloseButton.Click += new RoutedEventHandler(CloseButton_Click);
            ActiveVacation = in_oActiveVacation;
		}

        private void UpdateButtons()
        {
            if (ActiveVacation.Lists.Count > 0)
            {
                NewListButton.Visibility = Visibility.Visible;
                RenameListButton.Visibility = ListSelectDropDown.SelectedIndex > -1 ? Visibility.Visible : Visibility.Collapsed;
                PrintListButton.Visibility = ListSelectDropDown.SelectedIndex > -1 ? Visibility.Visible : Visibility.Collapsed;
                DeleteListButton.Visibility = ListSelectDropDown.SelectedIndex > -1 ? Visibility.Visible : Visibility.Collapsed;
                ListSelectDropDown.Visibility = Visibility.Visible;
            }
            else
            {
                NewListButton.Visibility = Visibility.Visible;
                RenameListButton.Visibility = Visibility.Collapsed;
                PrintListButton.Visibility = Visibility.Collapsed;
                DeleteListButton.Visibility = Visibility.Collapsed;
                ListSelectDropDown.Visibility = Visibility.Collapsed;
            }
        }

        void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (Close != null)
            {
                Log.TrackPageView("/NinePlaces/App"); 
                Close.Invoke(this, new EventArgs());
            }
        }

        void DoneRenameButton_Click(object sender, RoutedEventArgs e)
        {
            this.Focus();
            EndRename();
        }

        Popup pAreYouSure = null;
        void DeleteListButton_Click(object sender, RoutedEventArgs e)
        {
            AreYouSureDialog dlg = new AreYouSureDialog("DeleteListDangerous");

            pAreYouSure = new Popup();
            pAreYouSure.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            pAreYouSure.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            dlg.Close += new EventHandler(dlg_Close);

            pAreYouSure.Child = dlg;
            pAreYouSure.IsOpen = true;
            pAreYouSure.CenterPopup();
        }


        private void UpdatListNavURL()
        {

            string strURL = Helpers.GetAPIURL() + "List/";

            PrintListButton.TargetName = "_newNinePlacesWindow";
            if (ListSelectDropDown.SelectedIndex == -1)
                PrintListButton.NavigateUri = null;
            else
                PrintListButton.NavigateUri = new Uri(strURL + (ListSelectDropDown.SelectedValue as IListViewModel).UniqueID + "?AuthToken=" + (ListSelectDropDown.SelectedValue as IListViewModel).AuthToken);
        }


        void dlg_Close(object sender, EventArgs e)
        {
            AreYouSureDialog d = sender as AreYouSureDialog;
            pAreYouSure.IsOpen = false;

            if (d.UserSaidYes)
            {
                IListViewModel vm = ListSelectDropDown.SelectedValue as IListViewModel;
                if (vm == null)
                    return;

                int nSelectedIndex = ListSelectDropDown.SelectedIndex;
                ActiveVacation.RemoveChild(vm);
                ListSelectDropDown.SelectedIndex = nSelectedIndex == ListSelectDropDown.Items.Count ? nSelectedIndex - 1 : nSelectedIndex;
            }
        }

        void RenameListButton_Click(object sender, RoutedEventArgs e)
        {
            BeginRename();
        }

        public void BeginRename()
        {
            ListSelectDropDown.Visibility = Visibility.Collapsed;
            RenameStackPanel.Visibility = Visibility.Visible;
            RenameTextbox.SelectAll();
            RenameTextbox.Focus();
            RenameTextbox.LostFocus += new RoutedEventHandler(RenameTextbox_LostFocus);
            RenameTextbox.KeyDown += new KeyEventHandler(RenameTextbox_KeyDown);
        }

        public void RenameTextbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender == RenameTextbox)
            {
                EndRename();
                this.Focus();
            }
        }

        public void EndRename()
        {
            this.Focus();
            ListSelectDropDown.Visibility = Visibility.Visible;
            RenameStackPanel.Visibility = Visibility.Collapsed;
            RenameTextbox.LostFocus -= new RoutedEventHandler(RenameTextbox_LostFocus);
            RenameTextbox.KeyDown -= new KeyEventHandler(RenameTextbox_KeyDown);
        }

        void RenameTextbox_LostFocus(object sender, RoutedEventArgs e)
        {
            EndRename();
            UpdateButtons();
        }

        void ListSelectDropDown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //
            // set the listusercontrol datacontext.
            // it will draw out the list.
            //
            Todo.ActiveList = ListSelectDropDown.SelectedValue as IListViewModel;
            if (Todo.ActiveList != null)
                Log.TrackPageView("/NinePlaces/Lists/" + Todo.ActiveList.ListName.URLFriendly());
            else
                Log.TrackPageView("/NinePlaces/Lists");
            UpdateButtons();
            UpdatListNavURL();
            
        }

        void NewListButton_Click(object sender, RoutedEventArgs e)
        {
            IListViewModel vm = ActiveVacation.NewList();
            vm.ListName = "Untitled List";
            ListSelectDropDown.SelectedIndex = ListSelectDropDown.Items.Count - 1;
            Todo.AddNewItem();
            BeginRename();
        }
        
        void ListsDialog_LostFocus(object sender, RoutedEventArgs e)
        {
        }

        void ListsDialog_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
            Log.TrackPageView("/NinePlaces/Lists"); 
            if (ListSelectDropDown.Items.Count > 0)
            {
                ListSelectDropDown.SelectedIndex = 0;
            }
            UpdateButtons();
        }

        void ListsDialog_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && Close != null)
            {
                Log.TrackPageView("/NinePlaces/App"); 
                Close.Invoke(this, new EventArgs());
            }
        }
	}
}