using System.Windows.Controls;
using NinePlaces.ViewModels.IconViewModels;

namespace NinePlaces
{
    public partial class RestaurantDetailsEditor : UserControl, IDetailsEditor
    {
        public RestaurantDetailsEditor(IIconViewModel in_oVM)
        {
            // Required to initialize variables
            InitializeComponent();

            DataContext = in_oVM;
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