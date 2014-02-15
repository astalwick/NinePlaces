using System.Windows;
using System.Windows.Controls;
using NinePlaces.ViewModels.IconViewModels;

namespace NinePlaces
{
	public partial class CarDetailsEditor : UserControl, IDetailsEditor
	{
        public CarDetailsEditor(IIconViewModel in_oVM)
		{
			// Required to initialize variables
			InitializeComponent();

            DataContext = in_oVM;
            DepartureAddrCtrl.SizeChanged += new SizeChangedEventHandler(DepartureAddrCtrl_SizeChanged);
            DestinationAddrCtrl.SizeChanged += new SizeChangedEventHandler(DepartureAddrCtrl_SizeChanged);
		}

        void DepartureAddrCtrl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            LayoutRoot.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            this.Height = LayoutRoot.DesiredSize.Height;
        }

        public Control Control
        {
            get
            {
                return this as Control;
            }
        }
	}
}