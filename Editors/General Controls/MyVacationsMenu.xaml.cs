using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Common.Interfaces;

namespace Editors
{
	public partial class MyVacationsMenu : UserControl
	{
        public IVacationViewModel SelectedVacation { get; private set; }
        public event EventHandler VacationSelected;
        public event EventHandler NewVacationClicked;
        public event EventHandler LoadVacations;

        public MyVacationsMenu()
        {
            // Required to initialize variables
            InitializeComponent();
            LoadMyVacationsButton.Click += new RoutedEventHandler(LoadMyVacationsButton_Click);
            LoadMyVacationsButton.Visibility = Visibility.Visible;
            NewVacationButton.Visibility = Visibility.Collapsed;
        }

		public MyVacationsMenu(ITimelineViewModel in_oVM)
		{
			// Required to initialize variables
			InitializeComponent();

            Vacations.ItemsSource = in_oVM.Vacations;

            Vacations.SelectionChanged += new SelectionChangedEventHandler(Vacations_SelectionChanged);
            NewVacationButton.Click += new RoutedEventHandler(NewVacationButton_Click);
		}

        void LoadMyVacationsButton_Click(object sender, RoutedEventArgs e) 
        {
            SelectedVacation = null;
            if (LoadVacations != null)
            {
                LoadVacations.Invoke(this, new EventArgs());
            }
        }

        void NewVacationButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedVacation = null;
            if (NewVacationClicked != null)
            {
                NewVacationClicked.Invoke(this, new EventArgs());
            }
        }

        void Vacations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedVacation = (Vacations.SelectedItem as IVacationViewModel);
            if (VacationSelected != null)
            {
                VacationSelected.Invoke( this, new EventArgs());
            }
        }
	}
}