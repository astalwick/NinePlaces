using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Editors;
using System.Windows.Controls.Primitives;
using Common;

namespace NinePlaces
{
	public partial class VacationTitleControl : UserControl
	{
		public VacationTitleControl()
		{
			// Required to initialize variables
			InitializeComponent();
            Loaded += new RoutedEventHandler(VacationTitleControl_Loaded);
            ShareButton.Click += new RoutedEventHandler(ShareButton_Click);
            ListsButton.Click += new RoutedEventHandler(ListsButton_Click);
		}

        void ListsButton_Click(object sender, RoutedEventArgs e)
        {
            if (pSharing != null && pSharing.IsOpen == true)
                return;

            App.AssemblyLoadManager.Requires("Editors.xap");
            pSharing = new Popup();
            pSharing.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            pSharing.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            ListsDialog s = new ListsDialog(App.Timeline.ActiveVacation);
            s.Close += new EventHandler(s_Close);
            pSharing.Child = s;

            pSharing.IsOpen = true;
            pSharing.CenterPopup();
        }

        private void UpdateItineraryNavURL()
        {
            string strURL = Common.Helpers.GetAPIURL() + "Itinerary/";

            ItineraryButton.TargetName = "_newNinePlacesWindow";
            if (App.Timeline.ActiveVacation == null || App.LoginControl.VM.AuthToken == null)
                ItineraryButton.NavigateUri = null;
            else
                ItineraryButton.NavigateUri = new Uri(strURL + App.Timeline.ActiveVacation.UniqueID + "?AuthToken=" + App.LoginControl.VM.AuthToken);
        }


        private Popup pSharing = null;
        void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            if (pSharing != null && pSharing.IsOpen == true)
                return;
            App.AssemblyLoadManager.Requires("Editors.xap");
            pSharing = new Popup();
            pSharing.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            pSharing.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            SharingDialog s = new SharingDialog(App.LoginControl.VM.UserName, App.Timeline.ActiveVacation);
            s.Close += new EventHandler(s_Close);
            pSharing.Child = s;

            pSharing.IsOpen = true;
            pSharing.CenterPopup();
        }

        void s_Close(object sender, EventArgs e)
        {
            pSharing.IsOpen = false;
        }

        void SizePopop(Popup in_pToSize)
        {
            in_pToSize.Child.UpdateLayout();

            double dWidth = App.Current.Host.Content.ActualWidth;
            double dHeight = App.Current.Host.Content.ActualHeight;

            FrameworkElement fe = in_pToSize.Child as FrameworkElement;

            in_pToSize.VerticalOffset = (dHeight / 2.0) - (fe.DesiredSize.Height / 2.0);
            in_pToSize.HorizontalOffset = (dWidth / 2.0) - (fe.DesiredSize.Width / 2.0);
        }

        void VacationTitleControl_Loaded(object sender, RoutedEventArgs e)
        {
            App.LoginControl.VM.AuthenticationStatusChanged += new Common.Interfaces.AuthenticationEventHandler(VM_AuthenticationStatusChanged);
            App.Timeline.ActiveVacationChanged += new EventHandler(Timeline_ActiveVacationChanged);
            if (App.Timeline.ActiveVacation != null )
            {
                DataContext = App.Timeline.ActiveVacation;
            }
            EnableButtons();
        }

        private void EnableButtons()
        {
            if (App.Timeline.ActiveVacation == null || !App.Timeline.ActiveVacation.WritePermitted)
            {
                ListsButtonBorder.Visibility = ItineraryButtonBorder.Visibility = ShareButtonBorder.Visibility = Visibility.Collapsed;
                return;
            }

            ListsButtonBorder.Visibility = ItineraryButtonBorder.Visibility = ShareButtonBorder.Visibility = Visibility.Visible;
            if( App.Timeline.ActiveVacation != null && App.Timeline.ActiveVacation.WritePermitted && App.LoginControl.VM.Authenticated )
            {
                ListsButton.IsEnabled = ItineraryButton.IsEnabled = ShareButton.IsEnabled = true;
                ToolTipService.SetToolTip(ListsButtonBorder, string.Empty);
                ToolTipService.SetToolTip(ItineraryButtonBorder, string.Empty);
                ToolTipService.SetToolTip(ShareButtonBorder, string.Empty);
            }
            else
            {
                ListsButton.IsEnabled = ItineraryButton.IsEnabled = ShareButton.IsEnabled = false;
                ToolTipService.SetToolTip(ListsButtonBorder, "Sign-in to view and edit your lists!");
                ToolTipService.SetToolTip(ItineraryButtonBorder, "Sign-in to view and print your itinerary!");
                ToolTipService.SetToolTip(ShareButtonBorder, "Sign-in to share your vacation with others!");
            }

            UpdateItineraryNavURL();
        }

        void VM_AuthenticationStatusChanged(object sender, Common.Interfaces.AuthenticationEventArgs args)
        {
            EnableButtons();
        }

        void Timeline_ActiveVacationChanged(object sender, EventArgs e)
        {
            if (App.Timeline.ActiveVacation != null)
            {
                DataContext = App.Timeline.ActiveVacation;
                EnableButtons();
            }
            else
            {
                DataContext = null;
                EnableButtons();
            }
        }
	}
}