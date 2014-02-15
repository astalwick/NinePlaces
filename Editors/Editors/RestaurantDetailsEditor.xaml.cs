using System.Windows.Controls;
using Common.Interfaces;
using System.Windows;

namespace Editors
{
    public partial class RestaurantDetailsEditor : BaseDetailsEditor, IInterfaceElement
    {
        public RestaurantDetailsEditor(IIconViewModel in_oVM)
        {
            // Required to initialize variables
            InitializeComponent();

            DataContext = in_oVM;
            Address.SizeChanged += new SizeChangedEventHandler(Address_SizeChanged);
            IconClass = Common.Interfaces.IconClass.Activity;
		}

        void Address_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //LayoutRoot.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            //this.Height = LayoutRoot.DesiredSize.Height;
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