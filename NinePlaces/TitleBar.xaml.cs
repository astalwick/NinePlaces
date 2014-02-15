using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Editors.General_Controls;
using System.Globalization;
using Editors;
using System.Threading;
using System.Windows.Markup;
using Common;
using Common.Interfaces;


namespace NinePlaces
{
	public partial class TitleBar : UserControl
	{
		public TitleBar()
		{
			// Required to initialize variables
			InitializeComponent();
            ZoomButton.Click += new RoutedEventHandler(ZoomButton_Click);

            MyVacationsButton.Click += new RoutedEventHandler(MyVacationsButton_Click);
            App.SelectionMgr.SelectionCleared += new EventHandler(SelectionMgr_SelectionCleared);

            Loaded +=new RoutedEventHandler(TitleBar_Loaded);
		}

        void TitleBar_Loaded(object sender, RoutedEventArgs e)
        {
            App.LoginControl.VM.AuthenticationStatusChanged += new AuthenticationEventHandler(VM_AuthenticationStatusChanged);
        }

        void VM_AuthenticationStatusChanged(object sender, AuthenticationEventArgs args)
        {
            if (args.Authenticated)
                MyVacationsButton.Visibility = Visibility.Visible;
            else
                MyVacationsButton.Visibility = Visibility.Collapsed;
        }

        void MyVacationsButton_Click(object sender, RoutedEventArgs e)
        {
#warning this is a bit wiry, no?  should be cleaner.  App should expose the IsAuthenticated property, or something.
            if (App.LoginControl.VM.Authenticated)
            {
                App.AssemblyLoadManager.Requires("Editors.xap");

                MyVacationsMenu oMenu ;
                if (App.Timeline.VM.WritePermitted)
                    oMenu = new MyVacationsMenu(App.Timeline.VM as ITimelineViewModel);
                else
                    oMenu = new MyVacationsMenu();

                PositioningCanvas.Children.Add(oMenu);

                GeneralTransform objGeneralTransform = MyVacationsButton.TransformToVisual(PositioningCanvas as UIElement);
                Point ptX = objGeneralTransform.Transform(new Point(0, 20));
                Canvas.SetLeft(oMenu, ptX.X);

                oMenu.VacationSelected += new EventHandler(MyVacationsMenu_VacationSelected);
                oMenu.NewVacationClicked += new EventHandler(MyVacationsMenu_NewVacationClicked);
                oMenu.LoadVacations += new EventHandler(oMenu_LoadVacations);
            }
        }

        void oMenu_LoadVacations(object sender, EventArgs e)
        {
            if (!(sender is MyVacationsMenu))
                throw new Exception();

            App.Timeline.LoadVacations();

            PositioningCanvas.Children.Clear();
        }

        void MyVacationsMenu_NewVacationClicked(object sender, EventArgs e)
        {
            if (!(sender is MyVacationsMenu))
                throw new Exception();

            App.Timeline.NewVacation();

            PositioningCanvas.Children.Clear();
        }

        void MyVacationsMenu_VacationSelected(object sender, EventArgs e)
        {
            if (!(sender is MyVacationsMenu))
                throw new Exception();

            MyVacationsMenu v = sender as MyVacationsMenu;
            App.Timeline.ShowVacation(v.SelectedVacation);

            PositioningCanvas.Children.Clear();
        }

        void ZoomButton_Click(object sender, RoutedEventArgs e)
        {
            App.AssemblyLoadManager.Requires("Editors.xap");

            ZoomSlider oSlider = new ZoomSlider();
            oSlider.Value = -App.Timeline.Zoom;

            PositioningCanvas.Children.Add(oSlider);
            oSlider.SliderValueChanged += new RoutedPropertyChangedEventHandler<double>(ZoomSlider_SliderValueChanged);
        }

        void SelectionMgr_SelectionCleared(object sender, EventArgs e)
        {
            PositioningCanvas.Children.Clear();
        }

        void ZoomSlider_SliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            App.Timeline.Zoom = -e.NewValue;
        }
	}
}