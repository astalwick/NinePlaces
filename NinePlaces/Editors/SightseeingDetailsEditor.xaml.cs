using System.Windows.Controls;
using NinePlaces.ViewModels.IconViewModels;

namespace NinePlaces
{


    public partial class SightseeingDetailsEditor : UserControl, IDetailsEditor
    {
        public SightseeingDetailsEditor(IIconViewModel in_oIconVM)
        {
            // Required to initialize variables
            InitializeComponent();
            DataContext = in_oIconVM;   
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